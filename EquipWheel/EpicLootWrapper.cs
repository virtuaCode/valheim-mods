using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace EquipWheel
{

    public class EpicLootWrapper
    {
        private Assembly EpicLootAssembly;
        private Type ItemBackgroundHelper;
        private Type EpicLoot;
        private Type ItemDataExtensions;

        private MethodInfo CreateAndGetMagicItemBackgroundImage;
        private MethodInfo GetMagicItemBgSprite;
        private MethodInfo UseMagicBackground;
        private MethodInfo GetRarityColor;
        private MethodInfo GetDecoratedName;
        public static EpicLootWrapper instance;


        private EpicLootWrapper()
        {
        }

        public static EpicLootWrapper CreateInstance()
        {
            PluginInfo pluginInfo;

            try
            {
                pluginInfo = Chainloader.PluginInfos["randyknapp.mods.epicloot"];
            } catch (KeyNotFoundException)
            {
                return null;
            }

            var wrapper = new EpicLootWrapper();
            var assembly = pluginInfo.Instance.GetType().Assembly;

            if (assembly == null)
            {
                throw new Exception("Assembly for EpicLoot cannot be resolved");
            }

            wrapper.EpicLootAssembly = assembly;

            var types = wrapper.EpicLootAssembly.GetExportedTypes();
            foreach (var t in types)
            {
                if (t.FullName == "EpicLoot.ItemBackgroundHelper")
                    wrapper.ItemBackgroundHelper = t;

                if (t.FullName == "EpicLoot.EpicLoot")
                    wrapper.EpicLoot = t;

                if (t.FullName == "EpicLoot.ItemDataExtensions")
                    wrapper.ItemDataExtensions = t;
            }

            if (wrapper.ItemBackgroundHelper == null)
                throw new Exception("Type EpicLoot.ItemBackgroundHelper cannot be resolved");

            if (wrapper.EpicLoot == null)
                throw new Exception("Type EpicLoot.EpicLoot cannot be resolved");

            if (wrapper.ItemDataExtensions == null)
                throw new Exception("Type EpicLoot.ItemDataExtensions cannot be resolved");

            wrapper.CreateAndGetMagicItemBackgroundImage = wrapper.ItemBackgroundHelper.GetMethod("CreateAndGetMagicItemBackgroundImage",
              new Type[] { typeof(GameObject), typeof(GameObject), typeof(bool) });

            if (wrapper.CreateAndGetMagicItemBackgroundImage == null)
                throw new Exception("Method CreateAndGetMagicItemBackgroundImage cannot be resolved");

            wrapper.GetMagicItemBgSprite = wrapper.EpicLoot.GetMethod("GetMagicItemBgSprite", new Type[] { });

            if (wrapper.GetMagicItemBgSprite == null)
                throw new Exception("Method GetMagicItemBgSprite cannot be resolved");

            wrapper.UseMagicBackground = wrapper.ItemDataExtensions.GetMethod("UseMagicBackground", new Type[] { typeof(ItemDrop.ItemData) });

            if (wrapper.UseMagicBackground == null)
                throw new Exception("Method UseMagicBackground cannot be resolved");

            wrapper.GetRarityColor = wrapper.ItemDataExtensions.GetMethod("GetRarityColor", new Type[] { typeof(ItemDrop.ItemData) });

            if (wrapper.GetRarityColor == null)
                throw new Exception("Method GetRarityColor cannot be resolved");

            wrapper.GetDecoratedName = wrapper.ItemDataExtensions.GetMethod("GetDecoratedName", new Type[] { typeof(ItemDrop.ItemData), typeof(string) });

            if (wrapper.GetDecoratedName == null)
                throw new Exception("Method GetDecoratedName cannot be resolved");


            instance = wrapper;

            return wrapper;

        }

        public string GetItemName(ItemDrop.ItemData item, Color color)
        {
            return (string)GetDecoratedName.Invoke(null, new object[] { item, "#" + ColorUtility.ToHtmlStringRGB(color) });
        }

        public string GetItemName(ItemDrop.ItemData item)
        {
            return GetItemName(item, EquipWheel.GetHighlightColor);
        }

        public Color GetItemColor(ItemDrop.ItemData item)
        {
            return (Color)GetRarityColor.Invoke(null, new object[] { item });
        }

        public void ModifyElement(EquipWheelUI.ElementData element, ItemDrop.ItemData item)
        {
            if (element == null || item == null)
                return;

            var magicItemTransform = element.m_go.transform.Find("magicItem");
            if (magicItemTransform != null)
            {
                var mi = magicItemTransform.GetComponent<Image>();
                if (mi != null)
                {
                    mi.enabled = false;
                }
            }

            var setItemTransform = element.m_go.transform.Find("setItem");
            if (setItemTransform != null)
            {
                var setItem = setItemTransform.GetComponent<Image>();
                if (setItem != null)
                {
                    setItem.enabled = false;
                }
            }

            var magicItem = (Image)CreateAndGetMagicItemBackgroundImage.Invoke(null, new object[] { element.m_go, element.m_equiped.gameObject, true });

            if ((bool)UseMagicBackground.Invoke(null, new object[] { item }))
            {
                magicItem.enabled = true;
                magicItem.sprite = (Sprite)GetMagicItemBgSprite.Invoke(null, new object[] { });
                magicItem.color = (Color)GetRarityColor.Invoke(null, new object[] { item });
            }

            var setItem2 = element.m_go.transform.Find("setItem");
            if (setItem2 != null && !string.IsNullOrEmpty(item.m_shared.m_setName))
            {
                var img = setItem2.GetComponent<Image>();
                img.enabled = true;
            }
        }
    }
}
