# Problem brief: per-step vJoy device suffix (`/N`) vs. single-device interface

Self-contained briefing for an agent. Everything needed is below; no prior
conversation is required. Paths are relative to the repository root
`D:\RSI\src\streamdeck-vjoy-w4rl0ck`. The C# project lives one level down in
`streamdeck-vjoy-w4rl0ck/`.

## Context

This is a fork of an Elgato Stream Deck plugin that maps Stream Deck keys to
vJoy virtual joystick inputs. Phase 1 of the fork adds a macro-sequence language
executed by a new `Macro` action. One step in that language is a vJoy button
press with an **optional device suffix**:

```
B5              press button 5 on the DEFAULT device
B5/2            press button 5 on device 2
B5,120ms        press, hold 120 ms, release
+B5             press-and-hold on the default device
+B5/2,5s        press-and-hold button 5 on device 2, auto-release after 5 s
-B5/2           release button 5 on device 2
```

The `/N` suffix names an explicit vJoy device id (1..16). When omitted, the step
targets the "default device". The language grammar therefore assumes a step can
address **any** vJoy device, not just one.

## The conflict

The plugin was built around exactly **one** acquired vJoy device. The device
suffix in the language implies **multiple simultaneously-acquired** devices. The
current shared interface cannot express "press a button on device 2 while device
1 is also held."

## Hard constraints (do not violate)

- **`AcquireVJD` is exclusive per process, but a single process MAY acquire
  several distinct vJoy devices (ids 1..16).** The exclusivity is across
  processes competing for the *same* device id, not a per-process cap of one.
- **Every Stream Deck plugin is its own process. All actions in this plugin
  share ONE `SimpleVJoyInterface` singleton.** New behaviour must go through that
  shared instance; actions must not acquire devices independently.
- **Held buttons outlive their sequence by design.** A held button is released
  only by an explicit `-B` step, its own timeout, `-all`, or teardown (action
  disposal / plugin shutdown / device release). A stuck-down button is the worst
  failure mode this plugin has; every teardown path must release holds for
  **every** acquired device.
- Files use CRLF. Diffs must contain only lines that actually changed.

## Current implementation — facts

### `streamdeck-vjoy-w4rl0ck/Utils/SimpleVJoyInterface.cs`

- Singleton, lazy (`Instance` at line 53). Constructor (lines 39-46) only does
  `new vJoy()` and checks `vJoyEnabled()`; it does **not** acquire a device.
- Holds exactly one acquired device: `public uint CurrentVJoyId` (line 48) and a
  single `vJoy.JoystickState _iReport` (line 36) that represents that one
  device's button/axis/POV state.
- `ConnectToVJoy(uint id)` (lines 169-211) is the only acquire path. It
  **relinquishes the previously held device** before acquiring a new one
  (`if (CurrentVJoyId > 0) DisconnectFromVJoy();`, line 176), then
  `AcquireVJD(id)` (line 189). Short-circuits when the requested id is unchanged
  and already owned (line 175).
- `DisconnectFromVJoy()` (lines 222-228) relinquishes `CurrentVJoyId` and zeroes
  it.
- `ButtonState(uint button, ButtonAction)` (lines 293-321) mutates the single
  `_iReport` and calls `UpdateVJoy()` (lines 213-220), which writes to
  `CurrentVJoyId` only. There is no device parameter anywhere in the button/axis
  API.
- `ConfiguredDevices()` (lines 71-91) already enumerates ids 1..16 and reports
  which exist/are free/busy — so device discovery exists, only acquisition and
  state are single-device.

### `streamdeck-vjoy-w4rl0ck/Utils/Configuration.cs`

- `GlobalSettings` has a single device id: `VJoyDeviceId` (JSON `"vjoy"`,
  line 9). One number, not a set.
- `ConfigurationUpdated()` (lines 93-96) calls
  `SimpleVJoyInterface.Instance.ConnectToVJoy(GlobalSettings.VJoyDeviceId)` — the
  single configured device is the only one ever acquired.

### Consequence

"Default device" = `GlobalSettings.VJoyDeviceId`. There is currently no code path
that acquires or writes to a second device, and the button state model
(`_iReport`) is single-device.

## The decision to make

How should Phase 1 handle the `/N` suffix, given the single-device interface?
Three candidate approaches were identified:

1. **Restrict to the configured device (lowest risk).**
   The parser understands `/N` syntactically, but execution only targets the
   configured default device. A `/N` that differs from `GlobalSettings.VJoyDeviceId`
   is a **validation error** surfaced in the Property Inspector. The shared
   `SimpleVJoyInterface` is left untouched. Full multi-device becomes a separate
   later phase. Language syntax is preserved for forward compatibility.

2. **Full multi-device support now.**
   Refactor `SimpleVJoyInterface` from one `CurrentVJoyId` + one `_iReport` into
   a keyed collection of acquired devices (id → per-device state), acquiring on
   demand and relinquishing all on teardown. Highest capability, but it changes a
   singleton that all six existing actions already depend on, and every teardown
   path must now release holds across all acquired devices.

3. **Parse and validate, defer execution.**
   Parse `/N`, validate the device exists in vJoy (`ConfiguredDevices()` already
   enumerates), but mark a step on a non-default device as "not yet executable" —
   a diagnostic rather than an error, with no runtime effect until a later phase.

## What the agent should produce

A recommendation among (1)/(2)/(3) — or a better option — with:

- The concrete change surface in `SimpleVJoyInterface.cs` and `Configuration.cs`
  (and `GlobalSettings` if multi-device is chosen).
- How teardown stays airtight for holds across every acquired device.
- What the parser/validator must reject vs. accept for `/N`.
- Migration/compatibility impact on the six existing actions that share the
  singleton (`Actions/*.cs`).

## Related decisions already fixed (context, not up for debate here)

- Repeated press of the same `Macro` key while its sequence is still running:
  **ignore** the new press (the running sequence finishes; no restart, no queue).
- `-all` scope: releases **only macro-managed holds** (tracked in a held-input
  registry), not buttons pressed by the existing Toggle/Simple actions.

## Findings: multi-device acquisition, measured 2026-07-28

Measured on the development machine with a throwaway console program (not
committed) referencing the same `vJoyInterfaceWrap.dll` as the plugin. vJoy
2.2.2 — dll and driver both report `0x222`, `DriverMatch` true. Devices 1 (128
buttons) and 2 (32 buttons) exist; the plugin was stopped first, so both were
`VJD_STAT_FREE`.

- **One process can hold several devices at once.** `AcquireVJD(1)` and
  `AcquireVJD(2)` from the same process both returned `true`, and acquiring the
  second did not disturb the first. The hard constraint above is confirmed as
  written: exclusivity is per device id across processes, not one device per
  process.
- **`GetVJDStatus` returns `VJD_STAT_OWN`** for a device held by the calling
  process, and `GetOwnerPid` returns that process's own pid. From a second
  process the same devices report `VJD_STAT_BUSY`, with `GetOwnerPid` returning
  the owner's pid — enough to name the blocking process in a diagnostic.
- **`UpdateVJD` routes by its `rID` argument; `JoystickState.bDevice` is
  ignored.** A report sent with `bDevice` set to a different device's id, and
  one sent with `bDevice = 0`, both landed on the device named by `rID` and left
  the other device untouched. The `_iReport.bDevice = (byte)CurrentVJoyId` line
  in `UpdateVJoy()` is thus harmless but not load-bearing. Per-device reports
  must still be separate `JoystickState` values, since each device carries its
  own button, axis and POV state.
- **Per-device state is independent.** Writing different button masks to the two
  devices and reading them back with `GetPosition` returned exactly what was
  written to each; button 1 was held down on both devices simultaneously.
- **`RelinquishVJD` is per device.** Each device returned to `VJD_STAT_FREE`,
  and releasing one did not affect the other.
- Caveat: this wrapper build declares `GetPosition` as returning `uint`, and it
  returned `0` on every call while correctly filling in the state. Its return
  value is not a usable success flag.

Consequence: the keyed-collection refactor — option (2), and the plan in
`docs/multidevice-support-plan.md` — rests on verified behaviour, and no
API-level obstacle to it remains. What stays unverified is teardown across
several acquired devices; that is Step 5 of that plan.
