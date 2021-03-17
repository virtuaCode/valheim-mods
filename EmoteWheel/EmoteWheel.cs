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

namespace EmoteWheel
{
    [BepInPlugin("virtuacode.valheim.emotewheel", "Emote Wheel Mod", "0.0.1")]
    public class EmoteWheel : BaseUnityPlugin
    {
        private Harmony harmony;
        public static ConfigEntry<string> Hotkey;
        public static ConfigEntry<bool> TriggerOnRelease;
        public static ConfigEntry<bool> TriggerOnClick;
        public static ConfigEntry<Color> HighlightColor;
        public static ConfigEntry<float> GuiScale;

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
            if (MyLogger == null)
                return;

            MyLogger.LogInfo(msg);
        }

        public static void LogErr(string msg)
        {
            if (MyLogger == null)
                return;

            MyLogger.LogError(msg);
        }
        public static void LogWarn(string msg)
        {
            if (MyLogger == null)
                return;

            MyLogger.LogWarning(msg);
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

            Hotkey = Config.Bind("General", "Hotkey", "t", "Hotkey for opening emote wheel menu");
            TriggerOnRelease = Config.Bind("General", "TriggerOnRelease", true, "Releasing the Hotkey will trigger the selected emote");
            TriggerOnClick = Config.Bind("General", "TriggerOnClick", false, "Click with left mouse button will trigger the selected emote");
            HighlightColor = Config.Bind("Appereance", "HighlightColor", new Color(1, 0.82f, 0), "Color of the highlighted selection");
            GuiScale = Config.Bind("Appereance", "GuiScale", 0.75f, "Scale factor of the user interface");

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Log(typeof(EmoteWheel).Name + " Loaded!");
        }

        void OnDestroy()
        {
            harmony?.UnpatchAll();
        }
    }

    public class EmoteGui : MonoBehaviour
    {
        EmoteWheelUI ui;
        public static AssetBundle assets;

        public static bool visible = false;
        public static bool inventoryVisible = false;


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

        private void Update()
        {

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
            {
                ui.gameObject.SetActive(false);
                visible = false;
                return;
            }



            bool canOpenMenu = (!EmoteGui.inventoryVisible) && (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen();
            if (!canOpenMenu)
            {
                ui.gameObject.SetActive(false);
                visible = false;
                return;
            }

            if (Input.GetKey(EmoteWheel.Hotkey.Value))
            {
                ui.gameObject.SetActive(true);
                visible = true;

                if (EmoteWheel.TriggerOnClick.Value && Input.GetMouseButtonDown(0))
                {

                    if (ui.CurrentEmote != null)
                    {
                        PlayEmote(ui.CurrentEmote);
                    }

                }
                return;

            }

            if (EmoteWheel.TriggerOnRelease.Value && Input.GetKeyUp(EmoteWheel.Hotkey.Value))
            {
                if (ui.CurrentEmote != null)
                {
                    PlayEmote(ui.CurrentEmote);
                }
            }

            visible = false;
            ui.gameObject.SetActive(false);
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


    public class CutoutMask : Image
    {
        public override Material materialForRendering
        {
            get
            {
                Material material = new Material(base.materialForRendering);
                material.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
                return material;
            }
        }
    }



    public class EmoteWheelUI : MonoBehaviour
    {
        /* Constants */
        private static readonly float ANGLE_STEP = 360f / 7f;
        private static readonly float INNER_DIAMETER = 430f;

        [System.Serializable]
        private class Item
        {

            public string name;
            public Sprite icon;
            public string command;
        }

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
                if (MouseInCenter)
                    return -1;

                return Mod((int)(Mathf.Round((Angle) / ANGLE_STEP)), items.Length);
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
                float radius = INNER_DIAMETER / 2 * EmoteWheel.GuiScale.Value;
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
            var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
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
