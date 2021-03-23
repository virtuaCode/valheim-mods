using System;
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
            var k = Array.IndexOf(NUMBERS, EquipWheel.Hotkey.Value);

            return k != index - 1 || !EquipGui.CanOpenMenu || ZInput.IsGamepadActive();
        }
    }
}
