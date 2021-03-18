using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace EquipWheel
{
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickX))]
    class GetJoyLeftStickX_Patch
    {
        public static void Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0)
                __result = 0;
        }
    }

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStickY))]
    class GetJoyLeftStickY_Patch
    {
        public static void Postfix(ref float __result)
        {
            if (EquipWheel.JoyStickIgnoreTime > 0)
                __result = 0;
        }
    }


}
