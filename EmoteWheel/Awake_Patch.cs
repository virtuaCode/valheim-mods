using HarmonyLib;
using UnityEngine;

namespace EmoteWheel
{
    [HarmonyPatch(typeof(Player), "Awake")]
    class Awake_Patch
    {
        public static void Postfix()
        {
            if (Menu.instance == null || GameObject.Find("EmoteGui"))
                return;

            GameObject g = new GameObject("EmoteGui");
            g.AddComponent<EmoteGui>();
            g.transform.parent = Menu.instance.transform.parent;

            Debug.Log("[EmoteWheel] Patch Menu!");
        }
    }
}
