using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace EquipWheel
{
    [BepInPlugin("virtuacode.valheim.equipwheel", "Equip Wheel Mod", "0.0.1")]
    public class EquipWheel : BaseUnityPlugin
    {
        private Harmony harmony;
        public static ConfigEntry<string> Hotkey;
        public static ConfigEntry<bool> TriggerOnRelease;
        public static ConfigEntry<bool> TriggerOnClick;
        public static ConfigEntry<Color> HighlightColor;
        public static ConfigEntry<float> GuiScale;
        public static ConfigEntry<bool> AutoEquipShield;
        public static ConfigEntry<bool> HideHotkeyBar;
        public static ConfigEntry<int> InventoryRow;

        public static ManualLogSource MyLogger;


        public static Color GetHighlightColor
        {
            get
            {
                return HighlightColor.Value;
            }
        }

        public static void Log(string msg)
        {
            MyLogger?.LogInfo(msg);
        }

        public static void LogErr(string msg)
        {
            MyLogger?.LogError(msg);
        }
        public static void LogWarn(string msg)
        {
            MyLogger?.LogWarning(msg);
        }


        public static bool IsDedicated()
        {
            var method = typeof(ZNet).GetMethod(nameof(ZNet.IsDedicated), BindingFlags.Public | BindingFlags.Instance);
            var openDelegate = (Func<ZNet, bool>)Delegate.CreateDelegate
                (typeof(Func<ZNet, bool>), method);
            return openDelegate(null);
        }

        public void Awake()
        {
            MyLogger = Logger;

            if (IsDedicated())
            {
                LogWarn("Mod not loaded because game instance is a dedicated server.");
                return;
            }

            Hotkey = Config.Bind("Input", "Hotkey", "g", "Hotkey for opening equip wheel menu");
            TriggerOnRelease = Config.Bind("Input", "TriggerOnRelease", true, "Releasing the Hotkey will equip/use the selected item");
            TriggerOnClick = Config.Bind("Input", "TriggerOnClick", false, "Click with left mouse button will equip/use the selected item");
            HighlightColor = Config.Bind("Appereance", "HighlightColor", new Color(0.414f, 0.734f, 1f), "Color of the highlighted selection");
            GuiScale = Config.Bind("Appereance", "GuiScale", 0.75f, "Scale factor of the user interface");
            HideHotkeyBar = Config.Bind("Appereance", "HideHotkeyBar", false, "Hides the top-left Hotkey Bar");
            AutoEquipShield = Config.Bind("Misc", "AutoEquipShield", true, "Enable auto equip of shield when one-handed weapon was equiped");
            InventoryRow = Config.Bind("Misc", "InventoryRow", 1, new ConfigDescription("Row of the inventory that should be used for the equip wheel", new AcceptableValueRange<int>(1, 4)));


            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Log(nameof(EquipWheel) + " Loaded!");
        }

        void OnDestroy()
        {
            harmony?.UnpatchAll();
        }
    }


    public class EquipGui : MonoBehaviour
    {
        EquipWheelUI ui;
        public static AssetBundle assets;

        public static bool visible = false;
        public static bool inventoryVisible = false;

        private HotkeyBar hotKeyBar;


        void Awake()
        {
            LoadAssets();
            GameObject uiPrefab = assets.LoadAsset<GameObject>("assets/selectionwheel/selectionwheel.prefab");

            var rect = gameObject.AddComponent<RectTransform>();

            var go = Instantiate<GameObject>(uiPrefab, new Vector3(0, 0, 0), transform.rotation, rect);
            ui = go.AddComponent<EquipWheelUI>();
            go.SetActive(false);

            visible = false;
            assets.Unload(false);


            EquipWheel.HideHotkeyBar.SettingChanged += (e, args) =>
            {
                if (Hud.instance == null)
                    return;

                HotkeyBar hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").GetComponent<HotkeyBar>();

                if (hotKeyBar == null)
                    return;

                hotKeyBar.gameObject.SetActive(!EquipWheel.HideHotkeyBar.Value);
            };
        }

        void Start()
        {
            var rect = gameObject.GetComponent<RectTransform>();

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0, 0);

            if (Hud.instance == null)
                return;

            HotkeyBar hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").GetComponent<HotkeyBar>();

            if (hotKeyBar == null)
                return;

            hotKeyBar.gameObject.SetActive(!EquipWheel.HideHotkeyBar.Value);
        }

        private void LoadAssets()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream emotewheel = asm.GetManifestResourceStream("EquipWheel.res.selectionwheel");

            using (MemoryStream mStream = new MemoryStream())
            {
                emotewheel.CopyTo(mStream);
                assets = AssetBundle.LoadFromMemory(mStream.ToArray());
            }
        }

        private bool TryEquipWithShield()
        {
            var localPlayer = Player.m_localPlayer;
            localPlayer.UseItem(null, ui.CurrentItem, false);

            var shieldEquipped = (localPlayer.GetLeftItem() != null &&
                                  localPlayer.GetLeftItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            var equipShield = !shieldEquipped && localPlayer.IsItemQueued(ui.CurrentItem) && ui.CurrentItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon;

            if (equipShield)
            {
                var items = localPlayer.GetInventory().GetAllItems();

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                        {
                            localPlayer.UseItem(null, item, false);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void Update()
        {
            Player localPlayer = Player.m_localPlayer;

            if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
            {
                ui.gameObject.SetActive(false);
                visible = false;
                return;
            }

            bool canOpenMenu = (!EquipGui.inventoryVisible) && (Chat.instance == null || !Chat.instance.HasFocus()) &&
                               !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance != null &&
                               !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() &&
                               !GameCamera.InFreeFly() && !Minimap.IsOpen();

            if (!canOpenMenu)
            {
                ui.gameObject.SetActive(false);
                visible = false;
                return;
            }

            if (Input.GetKey(EquipWheel.Hotkey.Value))
            {
                ui.gameObject.SetActive(true);
                visible = true;

                if (EquipWheel.TriggerOnClick.Value && Input.GetMouseButtonDown(0))
                {
                    if (ui.CurrentItem != null)
                    {
                        ui.Flash();

                        if (EquipWheel.AutoEquipShield.Value)
                            TryEquipWithShield();
                        else
                            localPlayer.UseItem(null, ui.CurrentItem, false);
                    }
                }
                return;
            }

            if (EquipWheel.TriggerOnRelease.Value && Input.GetKeyUp(EquipWheel.Hotkey.Value))
            {
                if (ui.CurrentItem != null)
                {
                    if (EquipWheel.AutoEquipShield.Value)
                        TryEquipWithShield();
                    else
                        localPlayer.UseItem(null, ui.CurrentItem, false);
                }
            }

            visible = false;
            ui.gameObject.SetActive(false);
        }
    }


    public class EquipWheelUI : MonoBehaviour
    {
        /* Contants */
        public static readonly float ANGLE_STEP = 360f / 8f;
        public static readonly float ITEM_DISTANCE = 295f;
        public static readonly float ITEM_SCALE = 2f;
        public static readonly float INNER_DIAMETER = 430f;

        private GameObject cursor;
        private GameObject highlight;
        private readonly ItemDrop.ItemData[] items = new ItemDrop.ItemData[8];
        private HotkeyBar hotKeyBar;
        private Transform itemsRoot;
        private bool addedListener = false;

        /* Definitions from original Valheim code. (They weren't public)*/
        private readonly List<ElementData> m_elements = new List<ElementData>();
        private GameObject m_elementPrefab;

        private class ElementData
        {
            public bool m_used;
            public GameObject m_go;
            public Image m_icon;
            public GuiBar m_durability;
            public Text m_amount;
            public GameObject m_equiped;
            public GameObject m_queued;
            public GameObject m_selection;
        }

        private int previous = -1;
        public int Current
        {
            get
            {
                if (MouseInCenter)
                    return -1;

                int index = Mod((int)Mathf.Round((-Angle) / ANGLE_STEP), 8);

                if (index >= items.Length)
                    return -1;

                return index;
            }
        }

        public ItemDrop.ItemData CurrentItem
        {
            get
            {
                if (Current < 0)
                    return null;

                return items[Current];
            }
        }

        public bool MouseInCenter
        {
            get
            {
                float radius = INNER_DIAMETER / 2 * EquipWheel.GuiScale.Value;
                var dir = Input.mousePosition - cursor.transform.position;
                return dir.magnitude <= radius;
            }
        }

        public float Angle
        {
            get
            {
                var dir = Input.mousePosition - cursor.transform.position;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
                return angle;
            }
        }

        public IEnumerator FlashCoroutine(float aTime)
        {
            var color = EquipWheel.GetHighlightColor;

            for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / aTime)
            {
                highlight.GetComponent<Image>().color = Color.Lerp(Color.white, color, t);
                yield return null;
            }
        }

        private int Mod(int a, int b)
        {
            return (a % b + b) % b;
        }

        public void Flash()
        {
            StartCoroutine(FlashCoroutine(0.4f));
        }

        private void Awake()
        {
            cursor = transform.Find("Cursor").gameObject;
            highlight = transform.Find("Highlight").gameObject;

            hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").gameObject.GetComponent<HotkeyBar>();
            
            m_elementPrefab = hotKeyBar.m_elementPrefab;

            itemsRoot = transform.Find("Items");

            var mat = highlight.GetComponent<Image>().material;
            mat.SetFloat("_Degree", ANGLE_STEP);

        }

        private void Start()
        {
            if (!addedListener)
            {
                var player = Player.m_localPlayer;
                var inventory = player.GetInventory();

                inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnInventoryChanged));
                addedListener = true;
            }
        }

        public void OnInventoryChanged()
        {
            if (!EquipGui.visible)
            {
                return;
            }

            if (Player.m_localPlayer == null)
                return;

            var inventory = Player.m_localPlayer.GetInventory();

            if (inventory == null)
                return;

            for (int index = 0; index < 8; index++)
            {
                items[index] = null;
                var item = inventory.GetItemAt(index, EquipWheel.InventoryRow.Value - 1);

                if (item != null)
                    items[index] = item;

            }

            UpdateIcons(Player.m_localPlayer, true);
        }

        void OnEnable()
        {
            if (Player.m_localPlayer == null)
                return;

            var inventory = Player.m_localPlayer.GetInventory();

            if (inventory == null)
                return;

            for (int index = 0; index < 8; index++)
            {
                items[index] = null;
                var item = inventory.GetItemAt(index, EquipWheel.InventoryRow.Value - 1);

                if (item != null)
                    items[index] = item;
            }


            highlight.GetComponent<Image>().color = EquipWheel.GetHighlightColor;

            var scale = EquipWheel.GuiScale.Value;

            GetComponent<RectTransform>().localScale = new Vector3(scale, scale, scale);

            UpdateIcons(Player.m_localPlayer, true);
            Update();
        }

        void OnDisable()
        {
            highlight.SetActive(false);
            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                image.color = new Color(0, 0, 0, 0.5f);

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            if (Current != previous)
            {
                if (CurrentItem != null)
                {
                    InventoryGui.instance.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
                }
                previous = Current;
            }

            highlight.SetActive(CurrentItem != null);

            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                image.color = CurrentItem == null ? new Color(0,0,0,0.5f) : EquipWheel.GetHighlightColor;

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(CurrentItem != null);
                }
            }

            cursor.transform.rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
            var highlightAngle = Current * ANGLE_STEP;
            highlight.transform.rotation = Quaternion.AngleAxis(-highlightAngle, Vector3.forward);

            UpdateIcons(Player.m_localPlayer);
        }

        private int CountItems(ItemDrop.ItemData[] items)
        {
            int count = 0;

            foreach (var item in items)
            {
                if (item != null)
                    count++;
            }

            return count;
        }

        /* Modified UpdateIcons Function from HotkeyBar class */
        private void UpdateIcons(Player player, bool forceUpdate = false)
        {
            if (!player || player.IsDead())
            {
                foreach (ElementData elementData in this.m_elements)
                {
                    UnityEngine.Object.Destroy(elementData.m_go);
                }
                this.m_elements.Clear();
                return;
            }

            if (this.m_elements.Count != CountItems(items) || forceUpdate)
            {
                foreach (ElementData elementData2 in this.m_elements)
                {
                    UnityEngine.Object.Destroy(elementData2.m_go);
                }
                this.m_elements.Clear();
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                        continue;

                    ElementData elementData3 = new ElementData();
                    elementData3.m_go = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, itemsRoot);
                    elementData3.m_go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

                    var x = Mathf.Sin(i * ANGLE_STEP * Mathf.Deg2Rad) * ITEM_DISTANCE;
                    var y = Mathf.Cos(i * ANGLE_STEP * Mathf.Deg2Rad) * ITEM_DISTANCE;

                    elementData3.m_go.transform.localScale = new Vector3(ITEM_SCALE, ITEM_SCALE, ITEM_SCALE);
                    elementData3.m_go.transform.localPosition = new Vector3(x, y, 0f);
                    elementData3.m_go.transform.Find("binding").GetComponent<Text>().text = (i + 1).ToString();
                    elementData3.m_icon = elementData3.m_go.transform.transform.Find("icon").GetComponent<Image>();
                    elementData3.m_durability = elementData3.m_go.transform.Find("durability").GetComponent<GuiBar>();
                    elementData3.m_amount = elementData3.m_go.transform.Find("amount").GetComponent<Text>();
                    elementData3.m_equiped = elementData3.m_go.transform.Find("equiped").gameObject;
                    elementData3.m_queued = elementData3.m_go.transform.Find("queued").gameObject;
                    elementData3.m_selection = elementData3.m_go.transform.Find("selected").gameObject;

                    if (EquipWheel.InventoryRow.Value > 1)
                    {
                        elementData3.m_go.transform.Find("binding").GetComponent<Text>().enabled = false;
                    }

                    this.m_elements.Add(elementData3);
                }
            }
            foreach (ElementData elementData4 in this.m_elements)
            {
                elementData4.m_used = false;
            }
            bool flag = ZInput.IsGamepadActive();
            int elem_index = 0;
            for (int j = 0; j < this.items.Length; j++)
            {
                if (this.items[j] == null)
                    continue;

                ItemDrop.ItemData itemData2 = this.items[j];
                ElementData elementData5 = this.m_elements[elem_index];
                elementData5.m_used = true;
                elementData5.m_icon.gameObject.SetActive(true);
                elementData5.m_icon.sprite = itemData2.GetIcon();
                elementData5.m_durability.gameObject.SetActive(itemData2.m_shared.m_useDurability);
                if (itemData2.m_shared.m_useDurability)
                {
                    if (itemData2.m_durability <= 0f)
                    {
                        elementData5.m_durability.SetValue(1f);
                        elementData5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
                    }
                    else
                    {
                        elementData5.m_durability.SetValue(itemData2.GetDurabilityPercentage());
                        elementData5.m_durability.ResetColor();
                    }
                }
                elementData5.m_equiped.SetActive(itemData2.m_equiped);
                elementData5.m_queued.SetActive(player.IsItemQueued(itemData2));
                if (itemData2.m_shared.m_maxStackSize > 1)
                {
                    elementData5.m_amount.gameObject.SetActive(true);
                    elementData5.m_amount.text = itemData2.m_stack.ToString() + "/" + itemData2.m_shared.m_maxStackSize.ToString();
                }
                else
                {
                    elementData5.m_amount.gameObject.SetActive(false);
                }

                elem_index++;
            }
            for (int k = 0; k < this.m_elements.Count; k++)
            {
                ElementData elementData6 = this.m_elements[k];
                elementData6.m_selection.SetActive(flag && k == Current);
                if (!elementData6.m_used)
                {
                    elementData6.m_icon.gameObject.SetActive(false);
                    elementData6.m_durability.gameObject.SetActive(false);
                    elementData6.m_equiped.SetActive(false);
                    elementData6.m_queued.SetActive(false);
                    elementData6.m_amount.gameObject.SetActive(false);
                }
            }
        }
    }
}
