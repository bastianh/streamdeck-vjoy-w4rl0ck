REM install.bat
setlocal
cd /d %~dp0
cd streamdeck-vjoy-w4rl0ck/bin/Debug

SET STREAM_DECK_FILE="C:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
SET PLUGIN_UUID=dev.w4rl0ck.streamdeck.vjoy.sdPlugin
SET PLUGINS_DIR=%APPDATA%\Elgato\StreamDeck\Plugins

taskkill /f /im streamdeck.exe
taskkill /f /im dev.w4rl0ck.streamdeck.vjoy.exe
timeout /t 2

REM Remove old plugin
if exist "%PLUGINS_DIR%\%PLUGIN_UUID%" rmdir "%PLUGINS_DIR%\%PLUGIN_UUID%" /s /q

REM Copy new plugin
xcopy "%PLUGIN_UUID%" "%PLUGINS_DIR%\%PLUGIN_UUID%\" /s /i /y

START "" %STREAM_DECK_FILE%
