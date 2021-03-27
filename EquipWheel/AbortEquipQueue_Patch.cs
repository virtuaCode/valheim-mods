using System.Reflection;
using HarmonyLib;

namespace EquipWheel
{
    [HarmonyPatch(typeof(Player), "AbortEquipQueue")]
    class AbortEquipQueue_Patch
    {
        public static bool Prefix(Player __instance)
        {
            var run = (bool) typeof(Player).GetField("m_run", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(__instance);

            return !(run && EquipWheel.EquipWhileRunning.Value);
        }
    }
}
