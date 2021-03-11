using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;

namespace ModifyDataPerSec
{
    [BepInPlugin("virtuacode.valheim.modifydatapersec", "Modify Data Per Sec Mod", "1.0.0")]

    public class ModifyDataPerSec : BaseUnityPlugin
    {
        private static ConfigEntry<int> _dataPerSec;


        private void Awake()
        {
            _dataPerSec = Config.Bind("General", "DataPerSec", 262144, "Maxial Bytes per second to send to the server");


            Logger.LogInfo("ModifyDataPerSecMod Loaded!");
            Harmony.CreateAndPatchAll(typeof(ModifyDataPerSec));
        }

        public static void Log(string msg)
        {
            Debug.Log(" [" + typeof(ModifyDataPerSec).Name + "] " + msg);
        }

        public static void LogErr(string msg)
        {
            Debug.LogError(" [" + typeof(ModifyDataPerSec).Name + "] " + msg);
        }

        [HarmonyPatch(typeof(ZDOMan), "AddPeer")]
        [HarmonyPostfix]
        static void AddPeer_Postfix(ref int ___m_dataPerSec)
        {
            Log("(AddPeer) Fix m_dataPerSec (" + _dataPerSec.Value + ")");
            ___m_dataPerSec = _dataPerSec.Value;
        }
    }
}
