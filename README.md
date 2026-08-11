# streamdeck-vJoy-w4rl0ck

### Why vJoy buttons instead of keyboard macros?

- the buttons can work while the game is in the background (alt-tabbed)
- supports pressing multiple buttons at once
- you can use the buttons while entering text in game (e.g. open chat)
- supports feeding into tools like joystick gremlin to change modes or have duplicate bindings

### Features

- Supports up to 128 buttons
- Supports every vJoy device you have configured, each key picking its own
- Simple buttons that trigger a vJoy button when pressed, as long as it is pressed
- Toggle buttons that trigger a vJoy button until it is pressed again. It can show a different Title/Image while the action is active.
- Buttons to control a joystick axis... ( up, down, reset to 0 )
- Axis can be used as slider (0 - 100%) or regular axis initialized with a center (-100% - +100%)
- Stream Deck+ dial support to control vJoy axis or to send vjoy button presses when rotated
- Buttons that can control up to 4 POVs, on devices whose POVs are set to `Continuous` in the vJoy Configurator

### Download

Downloads available in the [releases](https://github.com/bastianh/streamdeck-vjoy-w4rl0ck/releases) section.

### Requirements

- Windows 10 or newer
- The [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) must be installed. If the plugin fails to load after installing, install the runtime and restart the Stream Deck software.

### Changes

#### 0.2.0
* Default images for buttons showing the button id
* Removed native configuration window in favor of a stream deck config page

### vJoy Devices

The plugin drives any of the devices you have set up in the vJoy Configurator, not just one.

- `Open Configuration` on any key sets the **default device**. That is the device a key uses when its own `Device` setting is left at `Default`, and it is the only setting shared by all keys.
- Every key has a `Device` selector next to its button id, axis or POV. A key that names a device drives that one no matter what the default is, so two keys can feed two devices at the same time.
- A device is taken when a key first uses it and stays with the plugin while it runs. Devices no key asks for are left free for other applications.
- Taking a device resets it, and so does giving one back. A key that stops pointing at a button, POV or axis releases what it was holding there, unless another key still drives it.
- If a key's device does not exist or is already in use by another application, the key shows the Stream Deck alert badge when pressed, and the property inspector says which of the two it is. Enable the device in the vJoy Configurator and the key starts working, no restart needed.

### Available Actions

![vjoy_demo](https://github.com/bastianh/streamdeck-vjoy-w4rl0ck/assets/17590/f528fc4a-83e2-4eb5-9f27-414e96fe7b40) ![vjoy_demo2](https://github.com/bastianh/streamdeck-vjoy-w4rl0ck/assets/17590/b5d401df-d58a-4be9-be27-339b5b0f0a99)

#### Simple Button

A simple button is connected to a vJoy button and activates the button als long as it is pressed.
If used in a MultiAction the button is active for 100ms.

#### State Button :new:

A button with two states, each with its own vJoy button id. The image shows the button id of the state it is in. A short press sends the **other** state's button and switches to it, so a key showing button 11 sends button 22 when tapped. A long press sends the button on the image without switching states.

#### Toggle Button 

The toggle button activates the vjoy button when pressed. The vjoy button stays active until it is pressed again. The Action shows a second state while the vjoy button is active.
If there is a simple button controlling the same vjoy button on the same device it will also update the state when the simple button is pressed.
In MultiActions the state is toggled as usual.

The toggle button now also supports long-press now to trigger a different button id. :new:

#### Axis 

The Axis action can be used in the Steam Deck as buttons or in the Steam Deck+ as a dial.
When used at buttons it can control the axis, while pressed the axis will move until released or it can be set to set the axis to 0.
When used as a dial you can rotate the dial to control the axis and press the dial to reset to zero.
It's possible to change the sensivity of the action to change the speed the axis moves.
It's also possible to control the same joystick axis with multiple buttons or dials with different sensitivitys to have a finer control over the axis. Keys on the same axis of different devices are independent of each other.
In MultiActions, depending on the setting in the property inspector, it will reset the axis to zero or move the axis for one step.

### Any Problems?

If you have any issues please create a issue here in github. Please check the log in the plugin directory: `%appdata%\Elgato\StreamDeck\Plugins\dev.w4rl0ck.streamdeck.vjoy.sdPlugin\pluginlog.log` 

Two things worth checking first:

- A POV that does nothing is usually a device with no POVs, or with POVs set to `4 Directions`. The plugin drives continuous POVs; set them to `Continuous` in the vJoy Configurator.
- The Stream Deck software stops a plugin by killing it, with no chance to tidy up, so a vJoy button held at that moment stays held. It is cleared as soon as the plugin takes that device again, which happens on the next start.

### Note

- This project uses the DLLs from vJoy v2.2.2.0 http://github.com/BrunnerInnovation/vJoy/releases/tag/v2.2.2.0. 
It should also work with the vJoy version that is suggested by joystick gremlin. If you use joystick gremlin you should use their suggested vJoy version. 

- This plugin uses the the [BarRaider's Stream Deck Tools](https://github.com/BarRaider/streamdeck-tools) as framework for the StreamDeck integration.

- This plugin includes the [BarRaider's StreamDeck EasyPI](https://github.com/BarRaider/streamdeck-easypi) library

- This plugin uses icons from [svgrepo.com](https://www.svgrepo.com/): [Joystick](https://www.svgrepo.com/svg/344949/joystick) MIT Licensed from the Bootstrap UI collection.