# Plan: multi-device vJoy support

## Goal

Let the plugin drive any number of vJoy devices installed in the system instead
of exactly one. Internally this replaces the single acquired device
(`CurrentVJoyId` + one `JoystickState`) with a keyed collection of acquired
devices; in the UI it adds a per-key device selector to every action and turns
the hardcoded device list in the global settings into the real one reported by
vJoy.

## Assumptions / context

- Repository root is `streamdeck-vjoy-w4rl0ck/`; the C# project lives one level
  down in `streamdeck-vjoy-w4rl0ck/streamdeck-vjoy-w4rl0ck/`. All paths below are
  relative to the repository root.
- Branch `feature/multi-device-support`, currently one commit ahead of `main`
  (`ActionIds`), working tree clean.
- **There is no test project.** CI runs `dotnet build` and `dotnet test`, but the
  solution contains only the plugin. Every step is therefore verified by a build
  plus a named manual check in **vJoy Monitor** or `joy.cpl` → Properties.
- Build sequence (the plugin locks its own `.exe`):
  `streamdeck stop dev.w4rl0ck.streamdeck.vjoy` → `dotnet build` →
  `streamdeck restart dev.w4rl0ck.streamdeck.vjoy`.
- Per CLAUDE.md, **Claude does not commit**. The "Commit" line on each step is
  the message the human should use when they commit that step.
- Current single-device facts (`Utils/SimpleVJoyInterface.cs`):
  - `CurrentVJoyId` (line 48), one `vJoy.JoystickState _iReport` (line 36), one
    `_maxAxisValue` (line 37).
  - `ConnectToVJoy(uint id)` relinquishes the previous device before acquiring a
    new one (line 176) — the only acquire path.
  - `ButtonState` / `SetPovSwitch` / `SetAxis` / `MoveAxis` /
    `GetCurrentAxisValue` have **no** device parameter; all six actions call them
    unqualified.
  - `ConfiguredDevices()` (lines 71-91) already enumerates ids 1..16 and is
    **currently unused**. So is `Utils/VJoyDeviceListEntry.cs`.
- Current UI facts:
  - `PropertyInspector/Global.html` has a hardcoded `<select>` with options 1..6
    — devices 7..16 are unreachable today even in single-device mode.
  - `PropertyInspector/local.js` renders the status line from the plugin's
    `sendToPropertyInspector` payload (`payload.device`, `payload.status`) and
    forwards every websocket message to the child `Global.html` window.
  - `PropertyInspector/js/Global.js` only handles `didReceiveGlobalSettings`.
  - ~~`UpdateButtonSignal` is declared and invoked but has **no subscribers**.~~
    Wrong, found in Step 4: `ToggleButtonAction` subscribes to it and filters on
    the button id alone, which stops being enough once two devices are in play.
- `GlobalSettings.AxisConfiguration` (reset-to-zero vs reset-to-center per axis)
  stays **global**, shared by all devices. Making it per-device is not part of
  this plan.
- Every new file under `PropertyInspector/`, `Images/` or `assets/` needs a
  `<None Update="…"><CopyToOutputDirectory>PreserveNewest` entry in the `.csproj`.
- Files use CRLF; diffs must contain only lines that actually changed.

## Risks

- **A single process may not actually be able to hold several vJoy devices at
  once.** The whole plan rests on `AcquireVJD` being exclusive *per device id
  across processes*, not *one device per process*. The existing brief
  (`docs/multidevice-device-suffix-problem.md`) asserts this but nothing in this
  repository has ever exercised it. → **Resolved by Step 1**, which is nothing
  but that check, before any production code changes. Measured on 2026-07-28 and
  confirmed: one process holds several devices, `UpdateVJD` routes by its `rID`
  argument alone, and per-device state is independent.
- **`ref`-returning accessors constrain the shape of the per-device state.**
  `GetAxisReference` / `GetPovReference` return `ref` into `_iReport`. Per-device
  state must therefore live in a **class**, not a struct held in a dictionary, or
  those methods stop compiling. → Addressed explicitly in Step 3.
- **No shutdown hook exists today.** Nothing calls `DisconnectFromVJoy` except
  `ConnectToVJoy`. With N acquired devices, a plugin exit could leave buttons
  latched on several devices at once — the worst failure mode this plugin has.
  Whether StreamDeck-Tools 7.0.0 surfaces a usable shutdown event, and whether
  the plugin process gets a clean exit at all (vs. being killed, in which case
  vJoy releases the devices itself), is unverified. → **Resolved by Step 5**,
  which is scheduled immediately after the first step that can acquire a second
  device, and which must report what it found even if the answer is "the OS
  cleans up and no hook is reachable".
- **Property Inspector option lists are populated after settings are loaded.**
  `loadConfiguration` (EasyPI) applies saved settings on websocket open; the
  device list arrives later via `sendToPropertyInspector`. A device `<select>`
  built from that later payload will lose the saved selection unless the
  Property Inspector remembers it and reapplies. → Addressed in Step 2
  (Global.html) and Step 4 (action PIs); the helper is written once, in
  `PropertyInspector/js/helper.js`, and reused.
- **Encoder actions need a human at the hardware.** The dev machine has a
  Stream Deck + XL (4×9, 6 encoders) alongside a Stream Deck Classic 3×5, so
  `AxisDialButtonAction` and `DialButtonAction` (Steps 8-10) can be checked here
  — but only by the human turning a dial, not by any check Claude can run.

## Steps

### Step 1: Verify that one process can acquire several vJoy devices

- **Change:** No production code. Write a throwaway console program in the
  scratchpad (outside the repository) that references `vJoyInterfaceWrap.dll` and
  attempts `AcquireVJD(1)` and `AcquireVJD(2)` from the same process, then writes
  a button to each and relinquishes both. Record the outcome — including whether
  `UpdateVJD` needs `bDevice` set per report, and what `GetVJDStatus` reports for
  a device held by this same process — as a short findings section appended to
  `docs/multidevice-device-suffix-problem.md`. This is the assumption every later
  step is built on; the commit makes the answer part of the repository instead of
  a memory.
- **Files:** `docs/multidevice-device-suffix-problem.md` (append only). Scratch
  program is not committed.
- **Verify:** The scratch program prints success for both acquisitions, and both
  devices show the pressed button simultaneously in vJoy Monitor. If it fails,
  **stop** — Steps 3-12 are invalid and the plan needs rework.
- **Commit:** `docs: record verified multi-device vJoy acquisition behaviour`

### Step 2: Report the real vJoy device list to the Property Inspector

- **Change:** Extend `ConfiguredDevices()` to return the id **and** the
  `VjdStat` of each existing device, reusing the currently-dead
  `Utils/VJoyDeviceListEntry.cs` as the payload type. Add the resulting list to
  `Configuration.GetPropertyInspectorData()` as `devices`. Forward it into
  `Global.html`: `js/Global.js` gains a `sendToPropertyInspector` handler (the
  message is already forwarded to the child window by `local.js`) that rebuilds
  the device `<select>` from the payload and reapplies the saved value. Delivers
  real value on its own: devices 7..16 become selectable, and nonexistent devices
  stop being offered.
- **Done with three deviations:** the rebuild helper lives in
  `PropertyInspector/js/helper.js`, not in `local.js`, because `Global.html`
  loads the former and never the latter; `local.js` caches the last plugin
  payload and exposes `requestPluginData()`, since the child window opens after
  the plugin's only push and would otherwise stay empty until a vJoy status
  change that may never come; and a saved device vJoy no longer reports is kept
  as an option of its own instead of being dropped, so the setting does not look
  silently reset.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Utils/VJoyDeviceListEntry.cs`,
  `Utils/Configuration.cs`, `PropertyInspector/Global.html`,
  `PropertyInspector/js/Global.js`, `PropertyInspector/js/helper.js`,
  `PropertyInspector/local.js`
- **Verify:** Build, restart the plugin, open any key → `Open Configuration`. The
  device dropdown lists exactly the devices configured in vJoy Configurator
  (add/remove one and reopen to confirm), and the previously saved device is
  still selected.
- **Commit:** `vjoy: list the real configured devices in global settings`

### Step 3: Extract per-device state into a `VJoyDevice` class

- **Change:** Move `_iReport`, `_maxAxisValue`, the per-device status and the
  update lock out of `SimpleVJoyInterface` into a new `Utils/VJoyDevice.cs`
  (**a class**, so `GetAxisReference` / `GetPovReference` can keep returning
  `ref` into its fields), holding `Acquire` / `Relinquish` / `Update` /
  `ResetAxisAndPovs`. `SimpleVJoyInterface` keeps a dictionary keyed by device
  id, which for now never holds more than the one configured device;
  `CurrentVJoyId` and `Status` become a facade over that entry so all six actions
  compile untouched. Pure structural change, no behaviour change — the
  unavoidable scaffolding step, kept as narrow as possible and with zero public
  API movement.
- **Done with three deviations:** the state-mutating operations moved along with
  the state they mutate — `SetPovSwitch`, `SetAxis`, `MoveAxis`, `ButtonState`
  and `GetCurrentAxisValue` now live on `VJoyDevice`, which keeps the `ref`
  accessors and the update lock private to itself, while
  `SimpleVJoyInterface` keeps the identical public signatures, routes to the
  current device and raises `AxisSignal` / `UpdateButtonSignal` from the result;
  `Status` falls back to an interface-level field for the states that belong to
  no device (`Initialized`, `Deactivated`, `Disconnected`); and with no device
  acquired those calls are now no-ops instead of writing a report to device `0`
  and re-acquiring it.
- **Files:** `Utils/VJoyDevice.cs` (new), `Utils/SimpleVJoyInterface.cs`
- **Verify:** Build. Then, on a profile with one of each keypad action: simple
  button, toggle button and state button still register presses in vJoy Monitor;
  the POV key still moves the hat; switching the device in `Open Configuration`
  still moves output to the other device and shows `Connected`.
- **Commit:** `vjoy: extract per-device state into a VJoyDevice class`

### Step 4: Target a vJoy device per key, starting with Simple Button

- **Change:** Add device-addressed entry points to `SimpleVJoyInterface`:
  `GetOrAcquireDevice(uint id)` (acquires on demand, adds to the dictionary) and
  `ButtonState(uint deviceId, uint button, ButtonAction)`. `deviceId == 0` means
  "the default device" — `GlobalSettings.VJoyDeviceId` — so the existing two-arg
  overload stays and keeps every other action working. Wire the first call site:
  `SimpleButtonAction` gains a `device` setting (default `0`) and its Property
  Inspector gains a device `<select>` whose options are built from the `devices`
  payload added in Step 2, with a `Default` entry at value `0`. The
  build-and-reapply helper already exists in `PropertyInspector/js/helper.js`
  from Step 2, but no action Property Inspector loads that file yet, so each one
  needs a `<script src="js/helper.js">` tag added. This is the walking skeleton:
  after this step two keys can drive two different vJoy devices at the same time.
- **Done with three deviations:** `PropertyInspector/js/helper.js` cannot be
  loaded by an action Property Inspector at all — it declares `const debounce`,
  which `sdtools.common.js` already declares, and a redeclaration kills the
  whole script; the two device-selector helpers therefore moved to a new
  `PropertyInspector/js/devices.js` that both `Global.html` and the action
  Property Inspectors load. `UpdateButtonSignal` gained the device id and
  `ToggleButtonAction` now filters on it, because the assumption that the signal
  has no subscribers was wrong and a Simple Button on device 2 would otherwise
  flip the state icon of a Toggle Button on the default device sharing its
  button id. And the default device is now acquired on demand as well, since the
  two-argument `ButtonState` routes through `GetOrAcquireDevice`: a key whose
  device was busy at startup starts working as soon as it is free, without a
  plugin restart.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Actions/SimpleButtonAction.cs`,
  `Actions/ToggleButtonAction.cs`, `PropertyInspector/SimpleButtonAction.html`,
  `PropertyInspector/js/devices.js` (new), `PropertyInspector/js/helper.js`,
  `PropertyInspector/Global.html`, `PropertyInspector/local.js`,
  `streamdeck-vjoy-w4rl0ck.csproj`
- **Verify:** Build. Configure key A as Simple Button → device 1 button 1, key B
  → device 2 button 1. In vJoy Monitor with both devices open, pressing A lights
  only device 1 and pressing B only device 2; holding both shows both lit. An
  existing key configured before this change (no `device` in its settings) still
  drives the default device.
- **Commit:** `vjoy: allow a key to target a specific vJoy device`

### Step 5: Release every acquired device on teardown

- **Change:** Add `SimpleVJoyInterface.ReleaseAllDevices()` — for each acquired
  device: clear all buttons, return POVs and axes to their configured neutral,
  push one final `UpdateVJD`, then `RelinquishVJD`. Call it on plugin shutdown
  (investigate what StreamDeck-Tools 7.0.0 offers; fall back to
  `AppDomain.CurrentDomain.ProcessExit` in `Program.cs`) and when a device is
  dropped from the acquired set. Record in the commit message what the shutdown
  path actually turned out to be. Scheduled here, immediately after the first
  step that can hold more than one device, because a latched button across
  several devices is this plugin's worst failure mode.
- **Done, and the shutdown half of it is not achievable.** Measured on
  2026-08-06, Stream Deck 7.5.0.22885 with StreamDeck-Tools 7.0.0, by writing a
  file directly from each candidate hook and then running
  `streamdeck stop`:
  - code after `SDWrapper.Run(args)` — never reached, `Run` does not return;
  - `AppDomain.CurrentDomain.ProcessExit` — does not fire;
  - an action's `Dispose()` — does not fire, with three live instances on the
    profile (verified by a probe in the constructor, which does fire);
  - `PluginBase.Destroy()` — `sealed override` in `KeypadBase` / `EncoderBase`,
    so a plugin cannot override it at all.

  Stream Deck kills the process. The probe instrument itself was validated by a
  write at startup, which lands every time.

  Worse, the operating system does **not** clean up on the plugin's behalf.
  After the owning process is killed, the device becomes `VJD_STAT_FREE`
  immediately, but its report is left untouched: a button held at kill time was
  still reported as pressed by `joyGetPosEx` — the same view a game gets — for
  the whole 61 s the measurement ran. It clears only when something acquires
  that device again and resets it, which is what `VJoyDevice.Acquire` does. So a
  button stuck this way survives until the plugin restarts and takes the device
  back: at startup for the default device, at the first key press for a device
  acquired on demand.

  What the step does deliver: `Release` resets buttons, axes and POVs and pushes
  a final report before relinquishing, on the one teardown path that is
  reachable — a device dropped from the acquired set when the default device
  changes. `ReleaseAllDevices` and the `ProcessExit` hook were written and then
  dropped again: with no reachable caller they were dead code, and `Program.cs`
  is untouched by this step.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Utils/VJoyDevice.cs`
- **Verify:** ~~Hold a key that presses a button on device 2, and while it is
  held run `streamdeck stop dev.w4rl0ck.streamdeck.vjoy`.~~ That scenario cannot
  pass — see the measurement above — and is not a check. Instead: hold a key
  whose device is the current default and, without letting go, select a
  different default in `Open Configuration`. In vJoy Monitor the button on the
  old device goes up and its axes return to neutral, instead of staying lit with
  no owner. Confirmed 2026-08-06, together with two effects that follow from
  acquisition resetting a device: the new default is cleared as well, so a key
  holding a button there loses it too, and a key on neither device is untouched.
- **Commit:** `vjoy: reset a device before handing it back to vJoy`

### Step 6: Add per-key device selection to Toggle Button

- **Change:** Same treatment as Step 4 for `ToggleButtonAction`: a `device`
  setting defaulting to `0`, routed through the device-addressed `ButtonState`,
  plus the device `<select>` in its Property Inspector. Its long-press button
  uses the same device as the short press — one device per key, not per button
  id, since a key that straddles two devices has no sensible state icon.
- **Files:** `Actions/ToggleButtonAction.cs`,
  `PropertyInspector/ToggleButtonAction.html`
- **Verify:** Build. A Toggle Button key set to device 2 latches and unlatches
  its button on device 2 in vJoy Monitor, device 1 untouched; long press still
  fires the long-press button, also on device 2. A pre-existing toggle key still
  works against the default device.
- **Commit:** `vjoy: add per-key device selection to the toggle button`

### Step 7: Add per-key device selection to State Button

- **Change:** The same change for `TriggerButtonAction` (the "State Button": two
  states, two button ids, long press). Both button ids go to the key's device,
  for the same reason as Step 6. Separate from Step 6 because it is a separate
  file with its own state handling and `DisableAutomaticStates` logic.
- **Files:** `Actions/TriggerButtonAction.cs`,
  `PropertyInspector/TriggerButtonAction.html`
- **Verify:** Build. A State Button key set to device 2 alternates its two button
  ids on device 2 across presses; the long-press path still reaches the right
  button id; the key image still tracks the state.
- **Commit:** `vjoy: add per-key device selection to the state button`

### Step 8: Add per-key device selection to Dial Button

- **Change:** The same change for `DialButtonAction` (encoder-only): dial press,
  clockwise and counter-clockwise button ids all target the key's device.
- **Files:** `Actions/DialButtonAction.cs`,
  `PropertyInspector/DialButtonAction.html`
- **Verify:** Build; keypad-path regression check that nothing else broke. On the
  Stream Deck + XL, rotating and pressing the dial drives the selected device
  only.
- **Commit:** `vjoy: add per-key device selection to the dial button`

### Step 9: Route POV output through the selected device

- **Change:** Add `SetPovSwitch(uint deviceId, ushort pov, uint direction)`,
  keeping the existing signature as the default-device overload, and give
  `POVButtonAction` a `device` setting plus its Property Inspector selector.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Actions/POVButtonAction.cs`,
  `PropertyInspector/PovButtonAction.html`
- **Verify:** Build. A POV key set to device 2 moves the hat on device 2 in vJoy
  Monitor and leaves device 1 centred; `Sticky` still holds the direction.
- **Commit:** `vjoy: route POV output through the key's selected device`

### Step 10: Route axis output through the selected device

- **Change:** Add device ids to the axis API — `SetAxis`, `MoveAxis`,
  `GetCurrentAxisValue` — and to the `AxisSignal` event, so a key only reacts to
  feedback from its own device. `AxisDialButtonAction` gains a `device` setting,
  filters `AxisSignal` on device **and** axis, reads `_maxAxisValue` from its own
  device, and sends its dial and touch button presses to the same device. Left
  last of the action migrations because it is the only one with a feedback loop
  back into the key display. `AxisConfiguration` stays global by design.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Utils/VJoyDevice.cs`,
  `Actions/AxisDialButtonAction.cs`,
  `PropertyInspector/AxisDialButtonAction.html`
- **Verify:** Build. Two Axis keys on the same axis but different devices: moving
  one changes only its device's axis in vJoy Monitor, and each key's title
  percentage tracks its own device rather than both jumping together. Encoder
  behaviour needs the human at the Stream Deck + XL.
- **Commit:** `vjoy: route axis output and feedback through the key's device`

### Step 11: Report an unusable device on the key instead of failing silently

- **Change:** When a key targets a device that does not exist or cannot be
  acquired (busy — owned by another process), surface it: the per-device status
  list in the Property Inspector shows the reason for that key's device, and
  `KeyPressed` calls `Connection.ShowAlert()` rather than dropping the press. The
  status line in `local.js` changes from a single device to the key's device plus
  the acquired set. With N devices, "nothing happened" is now ambiguous in a way
  it never was with one device, which is what makes this its own step.
- **Files:** `Utils/SimpleVJoyInterface.cs`, `Utils/Configuration.cs`,
  `PropertyInspector/local.js`, the six `Actions/*.cs`
- **Verify:** Configure a key for a device id that is not enabled in vJoy
  Configurator: the PI shows it as not existent and the key shows the Stream Deck
  alert badge on press. Then enable that device and confirm the key starts
  working without restarting the plugin.
- **Commit:** `vjoy: alert on keys whose vJoy device is missing or busy`

### Step 12: Document multi-device usage

- **Change:** Update `README.md` with the per-key device selector and what the
  global device setting now means (the default for keys that do not choose one),
  and refresh the action tooltips in `manifest.json` accordingly. Needed before
  this goes upstream as a PR.
- **Files:** `README.md`, `manifest.json`
- **Verify:** Build; the tooltips render as expected when hovering the actions in
  the Stream Deck action list. Read the README top to bottom for statements that
  the earlier steps made untrue.
- **Commit:** `docs: describe per-key vJoy device selection`

## Out of scope

- The macro sequence language and its `/N` device suffix
  (`docs/multidevice-device-suffix-problem.md`, phases 1-5 in CLAUDE.md). This
  plan makes that suffix implementable; it does not implement it.
- Per-device `AxisConfiguration` — axis reset behaviour stays global.
- Hot-plug detection: the device list refreshes when the Property Inspector
  opens, not on a vJoy configuration change while it is open.
- The `-all` release escape hatch, the named sequence library, and keyboard
  steps.
- Renaming or renumbering existing global settings keys; `vjoy` keeps its name
  and its meaning becomes "default device".
