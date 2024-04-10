setlocal
cd /d %~dp0
cd streamdeck-vjoy-w4rl0ck/bin/Debug

SET OUTPUT_DIR="C:\TEMP"
SET DISTRIBUTION_TOOL="C:\Users\dafir\source\repos\DistributionTool.exe"
SET STREAM_DECK_FILE="C:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
SET STREAM_DECK_LOAD_TIMEOUT=7

taskkill /f /im streamdeck.exe
taskkill /f /im dev.w4rl0ck.streamdeck.vjoy.exe
timeout /t 2
del %OUTPUT_DIR%\dev.w4rl0ck.streamdeck.vjoy.streamDeckPlugin
%DISTRIBUTION_TOOL% -b -i dev.w4rl0ck.streamdeck.vjoy.sdPlugin -o %OUTPUT_DIR%
rmdir %APPDATA%\Elgato\StreamDeck\Plugins\dev.w4rl0ck.streamdeck.vjoy.sdPlugin /s /q
START "" %STREAM_DECK_FILE%
timeout /t %STREAM_DECK_LOAD_TIMEOUT%
%OUTPUT_DIR%\dev.w4rl0ck.streamdeck.vjoy.streamDeckPlugin


