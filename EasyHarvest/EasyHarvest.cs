using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;

namespace EasyHarvest
{
    [BepInPlugin("virtuacode.valheim.easyharvest", "Easy Harvest Mod", "0.0.1")]

    class EasyHarvest : BaseUnityPlugin
    {
        private static ConfigEntry<float> _harvestRange;
        private static ConfigEntry<string> _allowedCrops;
        public static readonly string _defaultCrops = "mushroom,carrot,berries,berry,turnip,barley";
        private static string[] _crops;


        private void Awake()
        {
            _harvestRange = Config.Bind("Settings", "HarvestRange", 1.25f, "The range in which crops gets harvested");
            _allowedCrops = Config.Bind("Settings", "AllowedCrops", _defaultCrops, "List of allowed items (comma separated) that can be harvested. (Items are harvested if the item name contains any of the strings as substring)");

            _crops = _allowedCrops.Value.ToLower().Replace(" ", "").Split(',');

            Logger.LogInfo("EasyHarvestMod Loaded!");
            Harmony.CreateAndPatchAll(typeof(EasyHarvest));
        }

        public static void Log(string msg)
        {
            Debug.Log("[" + typeof(EasyHarvest).Name + "] " + msg);
        }

        public static void LogErr(string msg)
        {
            Debug.LogError("[" + typeof(EasyHarvest).Name + "] " + msg);
        }


        public static bool ContainsAny(string text, string[] values)
        {
            text = text.ToLower();

            foreach (string val in values)
            {
                if (text.Contains(val))
                {
                    return true;
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        [HarmonyPostfix]
        public static void UpdatePlacement_Postfix(Player __instance, bool ___m_secondaryAttack, ItemDrop.ItemData ___m_rightItem, int ___m_removeRayMask, ZSyncAnimation ___m_zanim)
        {
            if (___m_secondaryAttack && ___m_rightItem != null && ___m_rightItem.m_shared != null)
            {
                if (___m_rightItem.m_shared.m_name == "$item_cultivator")
                {
                    RaycastHit raycastHit;
                    if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, ___m_removeRayMask) && Vector3.Distance(raycastHit.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                    {
                        bool canHarvest = false;

                        if (raycastHit.collider.GetComponent<Pickable>())
                        {
                            canHarvest = true;
                        }

                        if (!canHarvest && raycastHit.collider.GetComponent<Heightmap>())
                        {
                            if (TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, _harvestRange.Value))
                            {
                                canHarvest = true;
                            }
                        }


                        if (canHarvest)
                        {
                            if (___m_rightItem != null)
                            {
                                __instance.FaceLookDirection();
                                ___m_zanim.SetTrigger(___m_rightItem.m_shared.m_attack.m_attackAnimation);
                            }

                            // We hit something!                            
                            foreach (Collider collider in Physics.OverlapSphere(raycastHit.point, _harvestRange.Value))
                            {

                                Pickable component = collider.gameObject.GetComponent<Pickable>();
                                if (!(component == null))
                                {
                                    Log("Found Pickable");
                                    if (ContainsAny(collider.gameObject.name, _crops))
                                    {
                                        Log("Interact with plant: " + collider.gameObject.name);
                                        component.Interact(__instance, false, false);
                                    }
                                }
                            }

                            __instance.UseStamina(___m_rightItem.m_shared.m_attack.m_attackStamina);
                            if (___m_rightItem.m_shared.m_useDurability)
                            {
                                ___m_rightItem.m_durability -= ___m_rightItem.m_shared.m_useDurabilityDrain;
                            }
                        }
                    }
                }
            }
        }
    }
}
