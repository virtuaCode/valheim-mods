#!/bin/bash
rm EmoteWheel_Valheim.zip
cp bin/Release/net461/EmoteWheel.dll ./
zip EmoteWheel_Valheim.zip manifest.json README.md icon.png EmoteWheel.dll
rm EmoteWheel.dll
