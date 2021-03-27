using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace EquipWheel
{
    [HarmonyPatch(typeof(Humanoid), "UseItem")]
    class UseItem_Patch
    {

        public static void Postfix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
        {
            if (!EquipWheel.AutoEquipShield.Value)
                return;

            if (__instance.GetType() != typeof(Player))
                return;

            if (fromInventoryGui)
                return;

            var shieldEquipped = (__instance.GetLeftItem() != null &&
                                  __instance.GetLeftItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            var equipShield = !shieldEquipped && ((Player) __instance).IsItemQueued(item) && item.m_equiped == false && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon;

            if (equipShield)
            {
                var items = __instance.GetInventory().GetAllItems();

                foreach (var itemData in items)
                {
                    if (itemData != null)
                    {
                        if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                        {
                            __instance.UseItem(null, itemData, false);
                            return;
                        }
                    }
                }
            }
        }
    }
}
