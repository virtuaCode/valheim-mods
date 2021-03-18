## Overview

This mod adds an user interface for equipping/using items in an easier and faster way. The menu will be visible by holding the 'G' key (can be
changed). On gamepads it's the X button.

**New in version 1.1.0: Gamepad Support**
Rebinds the gampad X-button to open the equip wheel. The left joystick controls the selected item.


To trigger equipping, release the Hotkey after selecting an item. (When TriggerOnRelease = true)

To trigger equipping, press the left mouse button after selecting an item. (When TriggerOnClick = true)

If you equip an one-handed weapon, the shield (if available) will be equipped automatically. 

The mod allows also to choose a different inventory row to use for the equip wheel.
The top-left hotkey bar can be optionally disabled.

Info: This mod should only be installed on the client side.


## Requirements (Manual Installation)

BepInEx Modloader must be installed. Can be found here:

[https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

To install the mod just move the downloaded EquipWheel.dll to the `<valheim-folder>\BepInEx\plugins\` folder.


## Config

A config file will be created after running the game once while the mod is installed:

`<valheim-folder>\BepInEx\config\virtuacode.valheim.equipwheel.cfg`

    ## Settings file was created by plugin Equip Wheel Mod v1.0.3
    ## Plugin GUID: virtuacode.valheim.equipwheel

    [Appereance]

    ## Color of the highlighted selection
    # Setting type: Color
    # Default value: 6ABBFFFF
    HighlightColor = 6ABBFFFF

    ## Scale factor of the user interface
    # Setting type: Single
    # Default value: 0.75
    GuiScale = 0.75

    ## Hides the top-left Hotkey Bar
    # Setting type: Boolean
    # Default value: false
    HideHotkeyBar = false

    [Input]

    ## Hotkey for opening equip wheel menu
    # Setting type: String
    # Default value: g
    Hotkey = g

    ## Releasing the Hotkey will equip/use the selected item
    # Setting type: Boolean
    # Default value: true
    TriggerOnRelease = true

    ## Click with left mouse button will equip/use the selected item
    # Setting type: Boolean
    # Default value: false
    TriggerOnClick = false

    ## Duration in milliseconds for ignoring left joystick input after button release
    # Setting type: Int32
    # Default value: 300
    # Acceptable value range: From 0 to 2000
    JoyStickIgnoreDuration = 300

    [Misc]

    ## Enable auto equip of shield when one-handed weapon was equiped
    # Setting type: Boolean
    # Default value: true
    AutoEquipShield = true

    ## Row of the inventory that should be used for the equip wheel
    # Setting type: Int32
    # Default value: 1
    # Acceptable value range: From 1 to 4
    InventoryRow = 1

Of course you can have both TriggerOnRelease and TriggerOnClick set to true if you want.

## Source Code

[https://github.com/virtuaCode/valheim-mods](https://github.com/virtuaCode/valheim-mods)

## Change Log

- Version 1.1.0
    - Add gamepad support
    - Fix auto equip shield when one-hand weapon gets unequipped
- Version 1.0.4
    - Fix inverted logic for hiding hotkeybar
- Version 1.0.3
    - Add option to hide hotkey bar
    - Add option to choose different inventory row
    - Fix unequipping shild when it shouldn't
- Version 1.0.2
    - Change shader compilation and bundling (fixes pink textures)
    - Fix auto equip shield when switched from bow
- Version 1.0.1
    - Move asset unload right after GUI instantiation
- Version 1.0.0
    - Initial Release

