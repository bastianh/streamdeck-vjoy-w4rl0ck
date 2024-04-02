# streamdeck-vJoy-w4rl0ck

### Why vJoy buttons instead of keyboard macros?

- the buttons can work when the game is the active application (alt-tabbed)
- it's not necessary to find unused modifier/keyboard combinations
- supports pressing multiple buttons at once
- you can use buttons while entering text in game (e.g. chat)
- supports feeding into tools like joystick gremlin to change modes or have duplicate bindings

### Features

- Supports up to 128 buttons
- Simple buttons that trigger a vJoy button when pressed, as long as it is pressed
- Toggle buttons that trigger a vJoy button until it is pressed again. It can show a different Title/Image while the button is active.
- Stream Deck + dial support to control vJoy axis

### Any Problems?

If you have any issues please create a issue here in github. Please check the log in the plugin directory: `%appdata%\Elgato\StreamDeck\Plugins\dev.w4rl0ck.streamdeck.vjoy.sdPlugin\pluginlog.log` 

### Note

- This project uses the DLLs from vJoy v2.2.2.0 http://github.com/BrunnerInnovation/vJoy/releases/tag/v2.2.2.0. 
It should also work with the installed vJoy version that is suggested by joystick gremlin. 

- This plugin uses the the [BarRaider's Stream Deck Tools](https://github.com/BarRaider/streamdeck-tools) as framework for the StreamDeck integration.

- This plugin includes the [BarRaider's StreamDeck EasyPI](https://github.com/BarRaider/streamdeck-easypi) library

