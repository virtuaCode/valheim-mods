#!/bin/bash
rm TrashItems_Valheim.zip
cp bin/Release/net461/TrashItems.dll ./
zip TrashItems_Valheim.zip manifest.json README.md icon.png TrashItems.dll
rm TrashItems.dll
