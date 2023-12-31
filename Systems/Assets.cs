﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AutomationAge.Systems
{
    internal static class Assets
    {

        public static string AssetsPath = Path.Combine(Plugin.ModPath, "Assets");
        public static AssetBundle MainBundle { get; private set; }

        public static void Prepare()
        {
            MainBundle = AssetBundle.LoadFromFile(Path.Combine(AssetsPath, "automationage"));
        }

        public static Texture2D GetTexture2D(string name)
        {
            return MainBundle.LoadAsset<Texture2D>(name);
        }

        public static GameObject GetGameObject(string name)
        {
            return MainBundle.LoadAsset<GameObject>(name);
        }

        public static Sprite GetSprite(string name)
        {
            Texture2D tex = GetTexture2D(name);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
