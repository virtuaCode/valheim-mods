using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EquipWheel
{

    [HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem))]
    class UseHotbarItem_Patch
    {
        public static readonly KeyCode[] NUMBERS = new KeyCode[]
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8
        };

        public static bool Prefix(int index)
        {
            var notNumber = Array.IndexOf(NUMBERS, EquipWheel.Hotkey.Value.MainKey) != index - 1;
            var notDown = !EquipWheel.IsShortcutDown;

            return notNumber || notDown || !EquipGui.CanOpenMenu || ZInput.IsGamepadActive();
        }
    }
}
