using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EquipWheel
{
    [HarmonyPatch(typeof(Player), "Awake")]
    class Awake_Patch
    {
        public static void Postfix()
        {
            var objectName = "EquipGui (" + Assembly.GetExecutingAssembly().GetName().Name + ")";

            if (Menu.instance == null || GameObject.Find(objectName))
                return;

            GameObject g = new GameObject(objectName);
            g.AddComponent<EquipGui>();
            g.transform.SetParent(Menu.instance.transform.parent, false);

            EquipWheel.Log("Spawned EquipGui!");
        }
    }
}
