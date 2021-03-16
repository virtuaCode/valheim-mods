using HarmonyLib;


namespace EquipWheel
{
    [HarmonyPatch(typeof(InventoryGui), "IsVisible")]
    public class IsVisible_Patch
    {
        public static void Postfix(ref bool __result)
        {
            EquipGui.inventoryVisible = __result; 
            __result = __result || EquipGui.visible;
        }
    }
}
