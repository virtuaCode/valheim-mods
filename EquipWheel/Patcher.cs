
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

#if EQUIPWHEEL_ONE
namespace EquipWheel {
#endif

#if EQUIPWHEEL_TWO
namespace EquipWheelTwo {
#endif

#if EQUIPWHEEL_THREE
namespace EquipWheelThree {
#endif

#if EQUIPWHEEL_FOUR
namespace EquipWheelFour
{
#endif

    public class Patcher
    {
        /* Patches */
#if EQUIPWHEEL_ONE
        [HarmonyPatch(typeof(InventoryGui), "IsVisible")]
        [HarmonyPostfix]
        public static void IsVisible_Postfix(ref bool __result)
        {
            WheelManager.inventoryVisible = __result;
            __result = __result || WheelManager.AnyVisible;
        }


        [HarmonyPatch(typeof(Humanoid), "UseItem")]
        [HarmonyPostfix]
        public static void UseItem_Postfix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
        {
            if (!EquipWheel.AutoEquipShield.Value)
                return;

            if (__instance.GetType() != typeof(Player))
                return;

            if (fromInventoryGui)
                return;

            var shieldEquipped = (__instance.GetLeftItem() != null &&
                                  __instance.GetLeftItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            var equipShield = !shieldEquipped && ((Player)__instance).IsEquipActionQueued(item) && item.m_equiped == false && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon;

            if (equipShield)
            {
                var items = __instance.GetInventory().GetAllItems();

                int x = -1;
                int y = -1;
                ItemDrop.ItemData data = null;

                foreach (var itemData in items)
                {
                    if (itemData != null)
                    {
                        if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                        {
                            if (data == null || itemData.m_gridPos.y < y || itemData.m_gridPos.y == y && itemData.m_gridPos.x < x)
                            {
                                data = itemData;
                                x = data.m_gridPos.x;
                                y = data.m_gridPos.y;
                            }
                        }
                    }
                }

                if (data != null)
                {
                    __instance.UseItem(null, data, false);
                }
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickX))]
        [HarmonyPostfix]
        public static void GetJoyLeftStickX_Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0 && EquipWheel.UseRightThumbstick.Value ==false)
                __result = 0;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickY))]
        [HarmonyPostfix]
        public static void GetJoyLeftStickY_Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0 && EquipWheel.UseRightThumbstick.Value == false)
                __result = 0;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyRightStickX))]
        [HarmonyPostfix]
        public static void GetJoyRightStickX_Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0 && EquipWheel.UseRightThumbstick.Value == true)
                __result = 0;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyRightStickY))]
        [HarmonyPostfix]
        public static void GetJoyRightStickY_Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0 && EquipWheel.UseRightThumbstick.Value == true)
                __result = 0;
        }

        [HarmonyPatch(typeof(HotkeyBar), "Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(HotkeyBar __instance)
        {
            if (EquipWheel.HideHotkeyBar.Value)
                __instance.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(Player), "ClearActionQueue")]
        [HarmonyPrefix]
        public static bool Prefix(Player __instance)
        {
            var run = (bool)typeof(Player).GetField("m_run", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(__instance);

            return !(run && EquipWheel.EquipWhileRunning.Value);
        }
#endif

        [HarmonyPatch(typeof(Player), "Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix()
        {
            var objectName = "EquipGui (" + Assembly.GetExecutingAssembly().GetName().Name + ")";

            if (Menu.instance == null || GameObject.Find(objectName))
                return;

            GameObject g = new GameObject(objectName);
            var gui = g.AddComponent<EquipGui>();

            EquipWheel.Gui = gui;
            
            g.transform.SetParent(Menu.instance.transform.parent, false);

            EquipWheel.Log("Spawned EquipGui!");
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
            var notNumber = Array.IndexOf(NUMBERS, EquipWheel.Hotkey.Value.MainKey) != index - 1;
            var notDown = !EquipWheel.IsShortcutDown;

            return notNumber || notDown || !EquipWheel.CanOpenMenu || ZInput.IsGamepadActive();
        }
    }
}