using HarmonyLib;


namespace EquipWheel
{
        [HarmonyPatch(typeof(HotkeyBar), "Update")]
        class Update_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                if (EquipWheel.HideHotkeyBar.Value)
                    __instance.gameObject.SetActive(false);
            }
        }
}
