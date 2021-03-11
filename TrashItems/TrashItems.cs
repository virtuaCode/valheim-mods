using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System.IO;

namespace ValheimMod
{
    [BepInPlugin("virtuacode.valheim.trashitems", "Trash Items Mod", "1.0.0")]

    class TrashItems : BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> returnResources;
        public static bool _clickedTrash = false;
        public static InventoryGui _gui;
        public static Sprite trashSprite;
     
        private void Awake()
        {

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            returnResources = Config.Bind<float>("General", "ReturnResources", 1f, "Fraction of resources to return");

            Logger.LogInfo(typeof(TrashItems).Name + " Loaded!");

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream trashImg = asm.GetManifestResourceStream("TrashItems.res.trash.png");

            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            
            using (MemoryStream mStream = new MemoryStream())
            {
                trashImg.CopyTo(mStream);
                tex.LoadImage(mStream.ToArray());
                tex.Apply();
                trashSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(32, 32), 100);
            }
            
            Harmony.CreateAndPatchAll(typeof(TrashItems));
        }



        public static void Log(string msg)
        {
            Debug.Log("[" + typeof(TrashItems).Name + "] " + msg);
        }

        public static void LogErr(string msg)
        {
            Debug.LogError("[" + typeof(TrashItems).Name + "] " + msg);
        }
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        [HarmonyPostfix]
        public static void Show_Postfix(InventoryGui __instance, Text ___m_weight)
        {
            _gui = __instance;

            Transform playerInventory = __instance.m_weight.transform.parent.parent;

            Transform trash = playerInventory.Find("Trash");

            if (trash == null)
            {
                trash = UnityEngine.Object.Instantiate(playerInventory.Find("Armor"), playerInventory);


                RectTransform rect = trash.GetComponent<RectTransform>();

                rect.anchoredPosition -= new Vector2(0, 76);
                Transform tText = trash.Find("ac_text");

                if (!tText)
                {
                    LogErr("ac_text not found!");
                    return;
                }

                tText.GetComponent<Text>().text = "Trash";
                tText.GetComponent<Text>().color = Color.red;

                Transform tArmor = trash.Find("armor_icon");

                if (!tArmor)
                {
                    LogErr("armor_icon not found!");
                }

                tArmor.GetComponent<Image>().sprite = trashSprite;

                trash.SetSiblingIndex(0);
                trash.gameObject.name = "Trash";

                Button button = trash.gameObject.AddComponent<Button>();

                button.onClick.AddListener(new UnityAction(TrashItems.TrashItem));

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        public static void UpdateItemDrag_Postfix(InventoryGui __instance, ref GameObject ___m_dragGo, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount)
        {
            if (_clickedTrash && ___m_dragItem != null && ___m_dragInventory.ContainsItem(___m_dragItem))
            {
                Log($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_dropPrefab.name}");

                if (___m_dragAmount == ___m_dragItem.m_stack)
                {
                    Player.m_localPlayer.RemoveFromEquipQueue(___m_dragItem);
                    Player.m_localPlayer.UnequipItem(___m_dragItem, false);
                    ___m_dragInventory.RemoveItem(___m_dragItem);
                }
                else
                    ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
                Destroy(___m_dragGo);
                ___m_dragGo = null;
                __instance.GetType().GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
            }

            _clickedTrash = false;
        }


        public static void TrashItem()
        {
            if (_gui == null)
            {
                return;
            }

            _clickedTrash = true;

            _gui.GetType().GetMethod("UpdateItemDrag", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_gui, new object[] { });
        }
    }
}
