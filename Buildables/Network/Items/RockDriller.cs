using AutomationAge.Systems;
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems.Miner;

namespace AutomationAge.Buildables.Network.Items
{
    internal class RockDriller
    {
        public const string StorageRoot = "RockDrillerRoot";
        public const string StorageRootClassID = "RockDrillerStorage";
        public const int Width = 4;
        public const int Height = 4;

        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("RockDriller", "Rock Driller", "Attachment for the Rock Extruder. Automatically breaks down outcrops and picks up raw resources.")
            .WithIcon(SpriteManager.Get(TechType.Drill));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.ExteriorModules, TechCategory.ExteriorModule);
            prefab.SetRecipe(new RecipeData(new Ingredient(TechType.Drill, 1), new Ingredient(TechType.AdvancedWiringKit, 1))).WithCraftingTime(5.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("RockDriller");
            GameObject model = obj.transform.Find("RockDrillerModel").gameObject;
            GameObject container = obj.transform.Find("Container").gameObject;

            obj.AddComponent<Driller>();

            ConstructableFlags constructableFlags = ConstructableFlags.Outside | ConstructableFlags.AllowedOnConstructable;
            PrefabUtils.AddStorageContainer(container, StorageRoot, StorageRootClassID, Width, Height, true);
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }
    }
}
