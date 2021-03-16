using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EquipWheel
{
    [HarmonyPatch]
    public class UpdateMouseCapture_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(GameCamera).GetMethod("UpdateMouseCapture", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Postfix()
        {
            if (EquipGui.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = ZInput.IsMouseActive();
            }
        }
    }
}
