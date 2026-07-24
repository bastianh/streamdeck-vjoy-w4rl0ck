setlocal
cd /d %~dp0
cd streamdeck-vjoy-w4rl0ck/bin/Debug

SET OUTPUT_DIR=C:\TEMP
SET PLUGIN_DIR=dev.w4rl0ck.streamdeck.vjoy.sdPlugin
SET PLUGIN_PACKAGE=dev.w4rl0ck.streamdeck.vjoy.streamDeckPlugin
SET STREAM_DECK_FILE="C:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
SET STREAM_DECK_LOAD_TIMEOUT=7

where streamdeck >nul 2>nul
if errorlevel 1 (
    echo ERROR: Stream Deck CLI not found. Install it with: npm install -g @elgato/cli
    exit /b 1
)

taskkill /f /im streamdeck.exe
taskkill /f /im dev.w4rl0ck.streamdeck.vjoy.exe
timeout /t 2

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if exist "%OUTPUT_DIR%\%PLUGIN_PACKAGE%" del "%OUTPUT_DIR%\%PLUGIN_PACKAGE%"

call streamdeck pack "%PLUGIN_DIR%" -o "%OUTPUT_DIR%" --force --no-update-check
if errorlevel 1 (
    echo ERROR: streamdeck pack failed.
    exit /b 1
)

if exist "%APPDATA%\Elgato\StreamDeck\Plugins\%PLUGIN_DIR%" rmdir "%APPDATA%\Elgato\StreamDeck\Plugins\%PLUGIN_DIR%" /s /q
START "" %STREAM_DECK_FILE%
timeout /t %STREAM_DECK_LOAD_TIMEOUT%
"%OUTPUT_DIR%\%PLUGIN_PACKAGE%"
