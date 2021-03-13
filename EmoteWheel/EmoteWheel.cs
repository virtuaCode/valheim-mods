using BepInEx;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using System.Reflection;
using System.IO;
using UnityEngine.Rendering;
using BepInEx.Configuration;

namespace EmoteWheel
{
    [BepInPlugin("virtuacode.valheim.emotewheel", "Emote Wheel Mod", "1.1.0")]
    public class EmoteWheel : BaseUnityPlugin
    {
        private Harmony harmony;
        public static ConfigEntry<string> Hotkey;
        public static ConfigEntry<bool> TriggerOnRelease;
        public static ConfigEntry<bool> TriggerOnClick;


        public void Awake()
        {
            Hotkey = Config.Bind("General", "Hotkey", "t", "Hotkey for opening emote wheel menu");
            TriggerOnRelease = Config.Bind("General", "TriggerOnRelease", true, "Releasing the Hotkey will trigger the selected emote");
            TriggerOnClick = Config.Bind("General", "TriggerOnClick", false, "Click with left mouse button will trigger the selected emote");

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo(typeof(EmoteWheel).Name + " Loaded!");
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
            GameObject uiPrefab = assets.LoadAsset<GameObject>("assets/emotewheel/emotewheel.prefab");

            var go = Instantiate<GameObject>(uiPrefab, transform);

            ui = go.AddComponent<EmoteWheelUI>();

            go.SetActive(false);
            visible = false;
        }
        private void LoadAssets()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream emotewheel = asm.GetManifestResourceStream("EmoteWheel.res.emotewheel");

            using (MemoryStream mStream = new MemoryStream())
            {
                emotewheel.CopyTo(mStream);
                assets = AssetBundle.LoadFromMemory(mStream.ToArray());
            }
        }

        void OnDestroy()
        {
            assets.Unload(true);
        }

        private void Update()
        {
            if (Input.GetKey(EmoteWheel.Hotkey.Value))
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
                {
                    ui.gameObject.SetActive(false);
                    visible = false;
                    return;
                }
                if ((!EmoteGui.inventoryVisible) && (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen())
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
            }
            else if (EmoteWheel.TriggerOnRelease.Value && Input.GetKeyUp(EmoteWheel.Hotkey.Value))
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
        [System.Serializable]
        public class Item
        {

            public string name;
            public Sprite icon;
            public string command;
        }

        public GameObject cursor;
        public GameObject highlight;
        public GameObject textPrefab;
        public Item[] items;
        public Text[] itemTexts;
        public Color NormalColor = new Color(1, 1, 1, 0.5f);
        public Color HighlightColor = Color.white;
        public Color YellowColor = new Color(1, 0.8235295f, 0);

        private Animator animator;
        private int previous = -1;
        public int Current
        {
            get
            {
                if (MouseInCenter)
                    return -1;

                return Mod((int)(Mathf.Round((Angle) / angleStep)), items.Length);
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
                RectTransform rect = cursor.transform.Find("CursorCircle1").GetComponent<RectTransform>();

                float radius = Mathf.Max(rect.rect.width + 10, rect.rect.height + 10) / 2 * GetComponent<Canvas>().scaleFactor;
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

        private readonly float angleStep = 360f / 7f;

        private int Mod(int a, int b)
        {
            return (a % b + b) % b;
        }

        public void Flash()
        {
            animator.SetTrigger("Flash");
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();

            cursor = transform.Find("Cursor").gameObject;
            highlight = transform.Find("Highlight").gameObject;

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

            textPrefab = transform.Find("TextPrefab").gameObject;

            var cc2 = cursor.transform.Find("CursorCircle1/CursorCircle2");
            if (cc2 == null)
            {
                Debug.LogError("cc2 is null");
                return;
            }

            var mask = cc2.gameObject.AddComponent<CutoutMask>();
            try
            {
                mask.sprite = EmoteGui.assets.LoadAsset<Sprite>("assets/emotewheel/circle1.png");
            }
            catch (System.NullReferenceException)
            {
                Debug.LogError("Could not load assets/emotewheel/circle1.png");
                throw;
            }

            mask.useSpriteMesh = true;
            mask.color = YellowColor;

            itemTexts = new Text[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                var text = Instantiate(textPrefab, transform);

                text.transform.RotateAround(transform.position, Vector3.forward, i * angleStep);
                text.transform.localRotation = Quaternion.AngleAxis(0, Vector3.forward);
                text.GetComponent<Text>().text = items[i].name;
                text.SetActive(true);

                itemTexts[i] = text.GetComponent<Text>();
                itemTexts[i].color = NormalColor;
            }
        }

        void OnDisable()
        {
            highlight.SetActive(false);
            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                image.color = HighlightColor;

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
                image.color = Current < 0 ? HighlightColor : YellowColor;

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(Current > -1);
                }
            }


            cursor.transform.rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
            var highlightAngle = Current * angleStep;
            highlight.transform.rotation = Quaternion.AngleAxis(highlightAngle, Vector3.forward);
        }
    }


}
