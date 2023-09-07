using Nautilus.Assets;
using Nautilus.Crafting;
using static CraftData;
using Nautilus.Utility;
using Nautilus.Assets.Gadgets;
using UnityEngine;
using AutomationAge.Systems;
using AutomationAge.Systems.Network.Requesters;

namespace AutomationAge.Buildables.Network.Items
{
    internal static class ItemRequester
    {
        public const string HoverText = "ConfigureFilter";
        public const string StorageRoot = "ItemRequesterRoot";
        public const string StorageRootClassID = "ItemRequesterFilter";
        public const int Width = 2;
        public const int Height = 2;

        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("ItemRequester", "Item Requester", "When attached to a storage module, will request the set items from available interfaced containers.")
            .WithIcon(SpriteManager.Get(TechType.Locker));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            prefab.SetRecipe(new RecipeData(new Ingredient(TechType.Titanium, 2), new Ingredient(TechType.WiringKit, 1))).WithCraftingTime(2.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("ItemRequester");
            GameObject model = obj.transform.Find("ItemRequesterModel").gameObject;

            obj.AddComponent<NetworkItemRequester>();

            ConstructableFlags constructableFlags = ConstructableFlags.AllowedOnConstructable | ConstructableFlags.Base | ConstructableFlags.Wall;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            StorageContainer container = PrefabUtils.AddStorageContainer(obj, StorageRoot, StorageRootClassID, Width, Height, true);
            container.hoverText = HoverText;

            return obj;
        }
    }
}
