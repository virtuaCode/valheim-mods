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
    [BepInPlugin("virtuacode.valheim.emotewheel", "Emote Wheel Mod", "1.0.0")]
    public class EmoteWheel : BaseUnityPlugin
    {
        private Harmony harmony;
        public static ConfigEntry<string> Hotkey;
        public void Awake()
        {
            Hotkey = Config.Bind("General", "Hotkey", "t", "Hotkey for opening emote wheel menu");

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


                    if (Input.GetMouseButtonDown(0))
                    {

                        string emote = ui.CurrentEmote;
                        var player = Player.m_localPlayer;
                            

                        switch(emote)
                        {
                            case "point":
                                player.FaceLookDirection();
                                player.StartEmote(emote, true);
                                break;
                            case "sit":
                                player.StartEmote(emote, false);
                                break;
                            default:
                                player.StartEmote(emote, true);
                                break;
                        }

                        ui.Flash();
                    }
                        return;
			    }
            }
            visible = false;
            ui.gameObject.SetActive(false);
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
        public Color NormalColor;
        public Color HighlightColor;

        private Animator animator;
        private int previous = -1;
        public int Current
        {
            get
            {
                return Mod((int)(Mathf.Round((Angle) / angleStep)), items.Length);
            }
        }

        public string CurrentEmote
        {
            get
            {
                return items[Current].command;
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
            highlight =transform.Find("Highlight").gameObject;

            HighlightColor = Color.white;
            NormalColor = new Color(1, 1, 1, 0.5f);

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
            mask.color = new Color(1, 0.8235295f, 0);
            

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

        // Update is called once per frame
        void Update()
        {
            if (Current != previous)
            {
                if (previous > -1)
                    itemTexts[previous].color = NormalColor;
                itemTexts[Current].color = HighlightColor;
                previous = Current;
            }

            cursor.transform.rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
            var highlightAngle = Current * angleStep;
            highlight.transform.rotation = Quaternion.AngleAxis(highlightAngle, Vector3.forward);
        }
    }


}
