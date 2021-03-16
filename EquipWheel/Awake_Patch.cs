using HarmonyLib;
using UnityEngine;

namespace EquipWheel
{
    [HarmonyPatch(typeof(Player), "Awake")]
    class Awake_Patch
    {
        public static void Postfix()
        {
            if (Menu.instance == null || GameObject.Find("EquipGui"))
                return;

            GameObject g = new GameObject("EquipGui");
            g.AddComponent<EquipGui>();
            g.transform.SetParent(Menu.instance.transform.parent, false);

            EquipWheel.Log("Spawned EquipGui!");
        }
    }
}
