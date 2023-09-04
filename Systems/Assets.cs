using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AutomationAge.Systems
{
    internal static class Assets
    {

        public static string AssetsPath = Path.Combine(Plugin.ModPath, "Assets");
        public static AssetBundle MainBundle { get; private set; }

        public static Dictionary<string, Texture2D> Sprites { get; private set; } = new Dictionary<string, Texture2D>();
        public static Dictionary<string, GameObject> GameObjects { get; private set; } = new Dictionary<string, GameObject>();

        public static void Prepare()
        {
            MainBundle = AssetBundle.LoadFromFile(Path.Combine(AssetsPath, "automationage"));

            foreach (Texture2D tex in MainBundle.LoadAllAssets<Texture2D>())
            {
                Sprites[tex.name] = tex;
            }

            foreach (GameObject go in MainBundle.LoadAllAssets<GameObject>())
            {
                GameObjects[go.name] = go;
            }
        }

        public static Texture2D GetTexture2D(string name)
        {
            return Sprites[name];
        }

        public static GameObject GetGameObject(string name)
        {
            return GameObjects[name];
        }

        public static Sprite GetSprite(string name)
        {
            Texture2D tex = GetTexture2D(name);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
