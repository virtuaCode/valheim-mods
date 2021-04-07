using BepInEx;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using System.Reflection;
using System.IO;
using UnityEngine.Rendering;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EmoteWheel
{
    [BepInPlugin("virtuacode.valheim.emotewheel", "Emote Wheel Mod", "0.0.1")]
    public class EmoteWheel : BaseUnityPlugin
    {
        private static Harmony harmony;
        public static ConfigEntry<KeyboardShortcut> Hotkey;
        public static ConfigEntry<bool> TriggerOnRelease;
        public static ConfigEntry<bool> TriggerOnClick;
        public static ConfigEntry<Color> HighlightColor;
        public static ConfigEntry<float> GuiScale;
        public static ConfigEntry<int> IgnoreJoyStickDuration;
        public static ConfigEntry<bool> ToggleMenu;
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<string> ProtectedBindings;
        public static ConfigEntry<KeyboardShortcut> HotkeyGamepad;

        public static KeyCode replacedKey = KeyCode.None;
        public static List<string> replacedButtons = new List<string>();

        public static ManualLogSource MyLogger = BepInEx.Logging.Logger.CreateLogSource(Assembly.GetExecutingAssembly().GetName().Name);

        public static float JoyStickIgnoreTime = 0;
        public static bool inventoryVisible = false;

        public static Color GetHighlightColor => HighlightColor.Value;

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

        public static bool CanOpenMenu
        {
            get
            {
                Player localPlayer = Player.m_localPlayer;

                bool canOpenMenu = !(localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting()) &&
                                   (!inventoryVisible) && (Chat.instance == null || !Chat.instance.HasFocus()) &&
                                   !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance != null &&
                                   !TextViewer.instance.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen();
                return canOpenMenu;
            }
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


            ModEnabled = Config.Bind("General", "ModEnabled", true, "Enable mod when value is true");
            Hotkey = Config.Bind("General", "Hotkey", KeyboardShortcut.Deserialize("T"), "Hotkey for opening emote wheel menu");
            HotkeyGamepad = Config.Bind("Input", "HotkeyGamepad", KeyboardShortcut.Deserialize("JoystickButton2"),
                "Hotkey on gamepads for opening emote wheel menu");
            ProtectedBindings = Config.Bind("Input", "ProtectedBindings",
                "JoyTabLeft JoyTabRight JoyButtonA JoyButtonB JoyButtonX JoyButtonY",
                "Button bindings that should never be overriden");
            TriggerOnRelease = Config.Bind("General", "TriggerOnRelease", true, "Releasing the Hotkey will trigger the selected emote");
            TriggerOnClick = Config.Bind("General", "TriggerOnClick", false, "Click with left mouse button will trigger the selected emote");
            ToggleMenu = Config.Bind("Input", "ToggleMenu", false,
                "When enabled the emote wheel will toggle between hidden/visible when the hotkey was pressed");
            HighlightColor = Config.Bind("Appereance", "HighlightColor", new Color(1, 0.82f, 0), "Color of the highlighted selection");
            GuiScale = Config.Bind("Appereance", "GuiScale", 0.75f, "Scale factor of the user interface");
            IgnoreJoyStickDuration = Config.Bind("Input", "IgnoreJoyStickDuration", 300,
                new ConfigDescription("Duration in milliseconds for ignoring left joystick input after button release",
                    new AcceptableValueRange<int>(0, 2000)));

            if (!ModEnabled.Value)
            {
                LogWarn("Mod not loaded because it was disabled via config.");
                return;
            }

            HotkeyGamepad.SettingChanged += (sender, args) => { RestoreGamepadButton(); ReplaceGamepadButton(); };


            harmony = Harmony.CreateAndPatchAll(typeof(Patcher));
            Log(nameof(EmoteWheel) + " Loaded!");
        }

        void OnDestroy()
        {
            harmony?.UnpatchAll();
        }

        public static void ReplaceGamepadButton()
        {
            if (ZInput.instance != null)
            {
                var buttons = (Dictionary<string, ZInput.ButtonDef>)typeof(ZInput).GetField("m_buttons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ZInput.instance);
                var bindings = ParseTokens(ProtectedBindings.Value);

                foreach (KeyValuePair<string, ZInput.ButtonDef> entry in buttons)
                {
                    var keyCode = entry.Value.m_key;

                    if (Array.IndexOf(bindings, entry.Key) > -1)
                        continue;

                    if (keyCode != KeyCode.None && keyCode == HotkeyGamepad.Value.MainKey)
                    {
                        replacedButtons.Add(entry.Key);
                        replacedKey = keyCode;

                        ZInput.instance.Setbutton(entry.Key, KeyCode.None);
                    }
                }
            }
        }

        public static void RestoreGamepadButton()
        {
            if (ZInput.instance != null)
            {
                if (replacedButtons.Count > 0)
                {
                    foreach (var button in replacedButtons)
                    {
                        ZInput.instance.Setbutton(button, replacedKey);

                    }
                    replacedButtons.Clear();
                    replacedKey = KeyCode.None;
                }
            }
        }

        public static bool IsShortcutDown
        {
            get
            {
                var shortcut = ZInput.IsGamepadActive() ? HotkeyGamepad.Value : Hotkey.Value;
                var mainKey = shortcut.MainKey;
                var modifierKeys = shortcut.Modifiers.ToArray();

                return Input.GetKeyDown(mainKey) && modifierKeys.All(Input.GetKey);
            }
        }

        public static bool IsShortcutUp
        {
            get
            {
                var shortcut = ZInput.IsGamepadActive() ? HotkeyGamepad.Value : Hotkey.Value;
                var mainKey = shortcut.MainKey;
                var modifierKeys = shortcut.Modifiers.ToArray();

                return Input.GetKeyUp(mainKey) || modifierKeys.Any(Input.GetKeyUp);
            }
        }

        public static bool IsShortcutPressed
        {
            get
            {
                var shortcut = ZInput.IsGamepadActive() ? HotkeyGamepad.Value : Hotkey.Value;
                var mainKey = shortcut.MainKey;

                var modifierKeys = shortcut.Modifiers.ToArray();

                return Input.GetKey(mainKey) && modifierKeys.All(Input.GetKey);
            }
        }

        public static void ParseNames(string value, ref string[] arr)
        {
            if (ObjectDB.instance == null)
                return;

            var names = ParseTokens(value);
            var ids = new List<string>();

            foreach (var name in names)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(name);
                if (prefab != null)
                {
                    var item = prefab.GetComponent<ItemDrop>();
                    ids.Add(item.m_itemData.m_shared.m_name);
                }
            }

            arr = ids.Distinct().ToArray();
        }

        public static string[] ParseTokens(string value)
        {
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
            return value.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
        }

    }

    public class EmoteGui : MonoBehaviour
    {
        EmoteWheelUI ui;
        public static AssetBundle assets;

        public static bool visible = false;
        public int toggleVisible = 0;


        void Awake()
        {
            LoadAssets();
            GameObject uiPrefab = assets.LoadAsset<GameObject>("assets/selectionwheel/selectionwheel.prefab");

            var rect = gameObject.AddComponent<RectTransform>();

            var go = Instantiate<GameObject>(uiPrefab, new Vector3(0, 0, 0), transform.rotation, rect);
            ui = go.AddComponent<EmoteWheelUI>();
             
            go.SetActive(false);
            visible = false;

            assets.Unload(false);
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


            EmoteWheel.RestoreGamepadButton();
            EmoteWheel.ReplaceGamepadButton();
        }


        private void LoadAssets()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream emotewheel = asm.GetManifestResourceStream("EmoteWheel.res.selectionwheel");

            using (MemoryStream mStream = new MemoryStream())
            {
                emotewheel.CopyTo(mStream);
                assets = AssetBundle.LoadFromMemory(mStream.ToArray());
            }
        }

        public void Hide()
        {
            ui.gameObject.SetActive(false);
            visible = false;
            toggleVisible = 0;
        }


        private void Update()
        {

            if (EmoteWheel.JoyStickIgnoreTime > 0)
                EmoteWheel.JoyStickIgnoreTime -= Time.deltaTime;

            if (!EmoteWheel.CanOpenMenu)
            {
                Hide();
                return;
            }

            if (EmoteWheel.IsShortcutDown && EmoteWheel.ToggleMenu.Value && toggleVisible < 2)
                toggleVisible++;

            var toggleDown =
                EmoteWheel.IsShortcutDown && EmoteWheel.ToggleMenu.Value && toggleVisible == 2;
            var hotkeyRelease = EmoteWheel.IsShortcutUp && !EmoteWheel.ToggleMenu.Value;

            if ((EmoteWheel.ToggleMenu.Value && toggleVisible > 0 && !toggleDown) || (EmoteWheel.IsShortcutPressed &&  !EmoteWheel.ToggleMenu.Value))
            {

                ui.gameObject.SetActive(true);
                visible = true;

                if (EmoteWheel.TriggerOnClick.Value && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0)))
                {
                    if (ui.CurrentEmote != null)
                    {
                       PlayEmote(ui.CurrentEmote);
                    }
                }

                if (!toggleDown)
                    return;
            }


            if (EmoteWheel.TriggerOnRelease.Value && (toggleDown || hotkeyRelease))
            {
                if (ui.CurrentEmote != null)
                {
                   PlayEmote(ui.CurrentEmote);
                }

                EmoteWheel.JoyStickIgnoreTime = EmoteWheel.IgnoreJoyStickDuration.Value / 1000f;
            }

            Hide();
        }

        private void PlayEmote(string emote)
        {
            var player = Player.m_localPlayer;

            if (!player.IsSitting())
                StopEmote(player);

            switch (emote)
            {
                case "point":
                    player.FaceLookDirection();
                    player.StartEmote(emote, true);
                    break;
                case "sit":
                    if (player.IsSitting())
                        StopEmote(player);
                    else
                        player.StartEmote(emote, false);
                    break;
                default:
                    player.StartEmote(emote, true);
                    break;
            }

            ui.Flash();
        }
        private void StopEmote(Player player)
        {
            typeof(Player).GetMethod("StopEmote", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, new object[] { });
        }
    }
    
    public class EmoteWheelUI : MonoBehaviour
    {
        /* Constants */
        public readonly float ANGLE_STEP = 360f / 7f;
        public readonly float INNER_DIAMETER = 340f;

        [System.Serializable]
        private class Item
        {

            public string name;
            public string command;
        }

        private Font font;
        private GameObject cursor;
        private GameObject highlight;
        private GameObject textPrefab;
        private Transform itemsRoot;

        private Item[] items;
        private Text[] itemTexts;

        private Color NormalColor = new Color(1, 1, 1, 0.5f);
        private Color HighlightColor = Color.white;
        private int previous = -1;
        public int Current
        {
            get
            {
                if ((!ZInput.IsGamepadActive() && MouseInCenter) || JoyStickInCenter)
                    return -1;

                int index = Mod((int)Mathf.Round(Angle / ANGLE_STEP), 7);

                if (index >= items.Length)
                    return -1;

                return index;
            }
        }

        public bool JoyStickInCenter
        {
            get
            {
                var x = ZInput.GetJoyLeftStickX();
                var y = ZInput.GetJoyLeftStickY();
                return ZInput.IsGamepadActive() && x == 0 && y == 0;
            }
        }

        public string CurrentEmote
        {
            get
            {
                if (Current < 0)
                    return null;

                return items[Current].command;
            }
        }

        public bool MouseInCenter
        {
            get
            {
                float radius = INNER_DIAMETER / 2 * gameObject.transform.lossyScale.x;
                var dir = Input.mousePosition - cursor.transform.position;
                return dir.magnitude <= radius;
            }
        }

        public float Angle
        {
            get
            {
                if (ZInput.IsGamepadActive())
                {
                    var x = ZInput.GetJoyLeftStickX();
                    var y = -ZInput.GetJoyLeftStickY();

                    if (x != 0 || y != 0)
                        return Mathf.Atan2(y, x) * Mathf.Rad2Deg - 90;
                }


                var dir = Input.mousePosition - cursor.transform.position;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
                return angle;
            }
        }

        public IEnumerator FlashCoroutine(float aTime)
        {
            var color = EmoteWheel.GetHighlightColor;

            for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / aTime)
            {
                highlight.GetComponent<Image>().color = Color.Lerp(Color.white, color, t);
                yield return null;
            }
        }

        public GameObject BuildTextPrefab()
        {
            var go = new GameObject();
            var rect = go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            rect.sizeDelta = new Vector2(200, 100);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector3(0, 300, 0);
            text.fontSize = 40;
            text.color = Color.white;
            //var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            text.font = font;
            text.fontStyle = UnityEngine.FontStyle.Bold;
            text.alignByGeometry = true;
            text.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
            return go;
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
            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font.name == "AveriaSerifLibre-Bold")
                {
                    this.font = font;
                    break;
                }
            }

            cursor = transform.Find("Cursor").gameObject;
            highlight = transform.Find("Highlight").gameObject;
            textPrefab = BuildTextPrefab();

            items = new Item[]
            {
                new Item {name = "Challenge", command = "challenge"  },
                new Item {name = "Thumbs Up", command = "thumbsup"  },
                new Item {name = "Sit", command = "sit"  },
                new Item {name = "Cheer", command = "cheer"  },
                new Item {name = "Point", command = "point"  },
                new Item {name = "No No No", command = "nonono"  },
                new Item {name = "Wave", command = "wave"  },
            };

            itemsRoot = transform.Find("Items");

            itemTexts = new Text[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                var text = Instantiate(textPrefab);
                text.transform.SetParent(itemsRoot);

                text.transform.RotateAround(transform.position, Vector3.forward, i * ANGLE_STEP);
                text.transform.localRotation = Quaternion.AngleAxis(0, Vector3.forward);
                text.GetComponent<Text>().text = items[i].name;
                text.SetActive(true);

                itemTexts[i] = text.GetComponent<Text>();
                itemTexts[i].color = NormalColor;
            }
        }

        void OnEnable()
        {
            highlight.GetComponent<Image>().color = EmoteWheel.GetHighlightColor;
            var scale = EmoteWheel.GuiScale.Value;
            GetComponent<RectTransform>().localScale = new Vector3(scale, scale, scale);
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

        // Update is called once per frame
        void Update()
        {
            if (Current != previous)
            {
                if (previous > -1)
                    itemTexts[previous].color = NormalColor;

                if (Current > -1)
                {
                    itemTexts[Current].color = HighlightColor;
                    InventoryGui.instance.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
                }
                previous = Current;
            }

            highlight.SetActive(Current > -1);

            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                image.color = Current < 0 ? new Color(0, 0, 0, 0.5f) : EmoteWheel.GetHighlightColor;

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(Current > -1);
                }
            }


            cursor.transform.rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
            var highlightAngle = Current * ANGLE_STEP;
            highlight.transform.rotation = Quaternion.AngleAxis(highlightAngle, Vector3.forward);
        }
    }
}
