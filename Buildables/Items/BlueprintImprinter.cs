using AutomationAge.Systems;
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems.Blueprint;

namespace AutomationAge.Buildables.Items
{
    internal class BlueprintImprinter
    {
        public const string ItemRootName = "ItemRoot";

        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("BlueprintImprinter", "Blueprint Imprinter", "Encodes item data into a blueprint that can be used to identify any other item of the same type.")
            .WithIcon(SpriteManager.Get(TechType.Workbench));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            prefab.SetRecipe(
                new RecipeData(
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1)))
                .WithCraftingTime(10.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("BlueprintImprinter");
            GameObject model = obj.transform.Find("Model").gameObject;

            obj.AddComponent<BlueprintEncoder>();

            ConstructableFlags constructableFlags = ConstructableFlags.Inside | ConstructableFlags.Ground | ConstructableFlags.Rotatable;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            foreach (UniqueIdentifier uid in obj.GetComponentsInChildren<UniqueIdentifier>())
            {
                uid.classId = Info.ClassID;
            }

            return obj;
        }
    }
}
