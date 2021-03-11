using HarmonyLib;


namespace EmoteWheel
{
    [HarmonyPatch(typeof(InventoryGui), "IsVisible")]
    public class IsVisible_Patch
    {
        public static void Postfix(ref bool __result)
        {
            EmoteGui.inventoryVisible = __result; 
            __result = __result || EmoteGui.visible;
        }
    }
}
