# streamdeck-vJoy-w4rl0ck

### Why vJoy buttons instead of keyboard macros?

- the buttons can work while the game is in the background (alt-tabbed)
- supports pressing multiple buttons at once
- you can use the buttons while entering text in game (e.g. open chat)
- supports feeding into tools like joystick gremlin to change modes or have duplicate bindings

### Features

- Supports up to 128 buttons
- Simple buttons that trigger a vJoy button when pressed, as long as it is pressed
- Toggle buttons that trigger a vJoy button until it is pressed again. It can show a different Title/Image while the action is active.
- Buttons to control a joystick axis... ( up, down, reset to 0 )
- Axis can be used as slider (0 - 100%) or regular axis initialized with a center (-100% - +100%)
- Stream Deck+ dial support to control vJoy axis

### Available Actions

![vjoy_demo](https://github.com/bastianh/streamdeck-vjoy-w4rl0ck/assets/17590/f528fc4a-83e2-4eb5-9f27-414e96fe7b40) ![vjoy_demo2](https://github.com/bastianh/streamdeck-vjoy-w4rl0ck/assets/17590/b5d401df-d58a-4be9-be27-339b5b0f0a99)

#### Simple Button

A simple button is connected to a vJoy button and activates the button als long as it is pressed

#### Toggle Button 

The toggle button activates the vjoy button when pressed. The vjoy button stays active until it is pressed again. The Action shows a second state while the vjoy button is active.
If there is a simple button controlling the same vjoy button it will also update the state when the simple button is pressed.

#### Axis 

The Axis action can be used in the Steam Deck as buttons or in the Steam Deck+ as a dial.
When used at buttons it can control the axis, while pressed the axis will move until released or it can be set to set the axis to 0.
When used as a dial you can rotate the dial to control the axis and press the dial to reset to zero.
It's possible to change the sensivity of the action to change the speed the axis moves.
It's also possible to control the same joystick axis with multiple buttons or dials with different sensitivitys to have a finer control over the axis.

### Any Problems?

If you have any issues please create a issue here in github. Please check the log in the plugin directory: `%appdata%\Elgato\StreamDeck\Plugins\dev.w4rl0ck.streamdeck.vjoy.sdPlugin\pluginlog.log` 

### Note

- This project uses the DLLs from vJoy v2.2.2.0 http://github.com/BrunnerInnovation/vJoy/releases/tag/v2.2.2.0. 
It should also work with the vJoy version that is suggested by joystick gremlin. If you use joystick gremlin you should use their suggested vJoy version. 

- This plugin uses the the [BarRaider's Stream Deck Tools](https://github.com/BarRaider/streamdeck-tools) as framework for the StreamDeck integration.

- This plugin includes the [BarRaider's StreamDeck EasyPI](https://github.com/BarRaider/streamdeck-easypi) library

