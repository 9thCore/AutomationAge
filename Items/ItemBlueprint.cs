using Nautilus.Assets;
using Nautilus.Crafting;
using static CraftData;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using UnityEngine;
using AutomationAge.Systems;

namespace AutomationAge.Items
{
    internal static class ItemBlueprint
    {
        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("ItemBlueprint", "Item Blueprint", "Contains data about an item, such as its molecular structure. Can be read and used by filter systems.")
            .WithIcon(SpriteManager.Get(TechType.Silicone));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);
            CloneTemplate clone = new CloneTemplate(Info, TechType.Silicone);

            clone.ModifyPrefab += (GameObject obj) =>
            {
                obj.AddComponent<BlueprintIdentifier>();
            };

            prefab.SetGameObject(clone);
            prefab.SetRecipe(new RecipeData(new Ingredient(TechType.Silicone, 1), new Ingredient(TechType.Titanium, 4)))
                .WithCraftingTime(2.0f)
                .WithStepsToFabricatorTab("Personal", "Equipment")
                .WithFabricatorType(CraftTree.Type.Fabricator);

            prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);

            prefab.Register();
        }

    }
}
