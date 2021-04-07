
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EmoteWheel
{

    public class Patcher
    {
        /* Patches */
        [HarmonyPatch(typeof(InventoryGui), "IsVisible")]
        [HarmonyPostfix]
        public static void IsVisible_Postfix(ref bool __result)
        {
            EmoteWheel.inventoryVisible = __result;
            __result = __result || EmoteGui.visible;
        }


        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickX))]
        [HarmonyPostfix]
        public static void GetJoyLeftStickX_Postfix(ref float __result)
        {
            if (EmoteWheel.JoyStickIgnoreTime > 0)
                __result = 0;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickY))]
        [HarmonyPostfix]
        public static void GetJoyLeftStickY_Postfix(ref float __result)
        {
            if (EmoteWheel.JoyStickIgnoreTime > 0)
                __result = 0;
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix()
        {
            var objectName = "EquipGui (" + Assembly.GetExecutingAssembly().GetName().Name + ")";

            if (Menu.instance == null || GameObject.Find(objectName))
                return;

            GameObject g = new GameObject(objectName);
            var gui = g.AddComponent<EmoteGui>();

            g.transform.SetParent(Menu.instance.transform.parent, false);

            EmoteWheel.Log("Spawned EquipGui!");
        }


        public static readonly KeyCode[] NUMBERS = new KeyCode[]
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8
        };

        [HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem))]
        [HarmonyPrefix]
        public static bool UseHotbarItem(int index)
        {
            var notNumber = Array.IndexOf(NUMBERS, EmoteWheel.Hotkey.Value.MainKey) != index - 1;
            var notDown = !EmoteWheel.IsShortcutDown;

            return notNumber || notDown || !EmoteWheel.CanOpenMenu || ZInput.IsGamepadActive();
        }
    }
}