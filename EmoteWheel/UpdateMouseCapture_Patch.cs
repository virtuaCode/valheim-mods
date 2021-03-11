using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EmoteWheel
{
    [HarmonyPatch]
    public class UpdateMouseCapture_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            Debug.Log("Patch UpdateMouseCapture");
            return typeof(GameCamera).GetMethod("UpdateMouseCapture", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Postfix()
        {
            if (EmoteGui.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = ZInput.IsMouseActive();
            }
        }
    }
}
