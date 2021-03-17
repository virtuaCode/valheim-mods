using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System.IO;
using System;

namespace ValheimMod
{
    [BepInPlugin("virtuacode.valheim.trashitems", "Trash Items Mod", "0.0.1")]

    class TrashItems : BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> returnResources;
        public static bool _clickedTrash = false;
        public static InventoryGui _gui;
        public static Sprite trashSprite;
        public static Sprite bgSprite;
     
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

            trashSprite = LoadSprite("TrashItems.res.trash.png", new Rect(0, 0, 64, 64), new Vector2(32, 32));
            bgSprite = LoadSprite("TrashItems.res.trashmask.png", new Rect(0, 0, 96, 112), new Vector2(48, 56));

            Harmony.CreateAndPatchAll(typeof(TrashItems));
        }


        public static Sprite LoadSprite(string path, Rect size, Vector2 pivot, int units = 100)
        {

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream trashImg = asm.GetManifestResourceStream(path);

            Texture2D tex = new Texture2D((int) size.width, (int) size.height, TextureFormat.RGBA32, false, true);

            using (MemoryStream mStream = new MemoryStream())
            {
                trashImg.CopyTo(mStream);
                tex.LoadImage(mStream.ToArray());
                tex.Apply();
                return Sprite.Create(tex, size, pivot, units);
            }
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
        [HarmonyPrefix]
        public static void Show_Prefix(InventoryGui __instance)
        {
            _gui = __instance;

            Transform playerInventory = __instance.m_player.transform;
            Transform trash = playerInventory.Find("Trash");

            if (trash == null)
            {
                trash = Instantiate(playerInventory.Find("Armor"), playerInventory);

                RectTransform rect = trash.GetComponent<RectTransform>();

                rect.anchoredPosition -= new Vector2(0, 78);

                // Replace text and color
                Transform tText = trash.Find("ac_text");
                if (!tText)
                {
                    LogErr("ac_text not found!");
                    return;
                }
                tText.GetComponent<Text>().text = "Trash";
                tText.GetComponent<Text>().color = Color.red;


                // Replace armor with trash icon
                Transform tArmor = trash.Find("armor_icon");
                if (!tArmor)
                {
                    LogErr("armor_icon not found!");
                }
                tArmor.GetComponent<Image>().sprite = trashSprite;

                trash.SetSiblingIndex(0);
                trash.gameObject.name = "Trash";

                // Add trash ui button
                Button button = trash.gameObject.AddComponent<Button>();
                button.onClick.AddListener(new UnityAction(TrashItems.TrashItem));

                // Add border background
                Transform frames = playerInventory.Find("selected_frame");
                GameObject newFrame = Instantiate(frames.GetChild(0).gameObject, trash);
                newFrame.GetComponent<Image>().sprite = bgSprite;
                newFrame.transform.SetAsFirstSibling();
                newFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(-8, 22);
                newFrame.GetComponent<RectTransform>().anchoredPosition = new Vector2(6, 7.5f);

                // Add inventory screen tab
                UIGroupHandler handler = trash.gameObject.AddComponent<UIGroupHandler>();
                handler.m_groupPriority = 1;
                handler.m_enableWhenActiveAndGamepad = newFrame;
                _gui.m_uiGroups = _gui.m_uiGroups.AddToArray(handler);

                trash.gameObject.AddComponent<TrashHandler>();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        public static void UpdateItemDrag_Postfix(InventoryGui __instance, ref GameObject ___m_dragGo, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount)
        {
            if (_clickedTrash && ___m_dragItem != null && ___m_dragInventory.ContainsItem(___m_dragItem))
            {
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
            Log("Trash Items clicked!");

            if (_gui == null)
            {
                LogErr("_gui is null");
                return;
            }

            _clickedTrash = true;

            _gui.GetType().GetMethod("UpdateItemDrag", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_gui, new object[] { });
        }
    }

    public class TrashHandler : MonoBehaviour
    {
        private UIGroupHandler handler;
        private void Awake()
        {
            handler = this.GetComponent<UIGroupHandler>();
        }

        private void Update()
        {
            if (ZInput.GetButtonDown("JoyButtonA") && handler.IsActive())
            {
                TrashItems.TrashItem();
                // Switch back to inventory iab
                typeof(InventoryGui).GetMethod("SetActiveGroup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InventoryGui.instance, new object[] { 1 });
            }
        }
    }
}
