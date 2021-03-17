using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutoLoad
{
    [BepInPlugin("virtuacode.valheim.autoload", "Auto Load Mod", "0.0.1")]

    class AutoLoad : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(AutoLoad));
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        [HarmonyPostfix]
        public static void Start_Patch(FejdStartup __instance, ref World ___m_world)
        {
            var lastWorld = PlayerPrefs.GetString("world");
            var worlds = World.GetWorldList();

            Debug.Log("Last world = " + lastWorld);

            if (lastWorld == null)
            {
                return;
            }

            foreach (var world in worlds)
            {
                if (world.m_name == lastWorld)
                {
                    Debug.LogWarning("Found world: " + lastWorld);
                    ___m_world = world;
                    PlayerProfile p = PlayerProfile.GetAllPlayerProfiles()[0];
                    Game.SetProfile(p.GetFilename());
                    __instance.OnWorldStart();
                    return;
                }
            }
        }
    }

}