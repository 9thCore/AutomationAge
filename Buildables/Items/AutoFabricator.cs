using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems;
using AutomationAge.Systems.AutoCrafting;
using AutomationAge.Systems.Network;

namespace AutomationAge.Buildables.Items
{
    internal class AutoFabricator
    {
        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("AutoFabricator", "Automatic Fabricator", "Automatically processes ingredients into a given result. Requires a recipe blueprint.")
            .WithIcon(SpriteManager.Get(TechType.Fabricator));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            prefab.SetRecipe(
                new RecipeData(
                    new Ingredient(TechType.Titanium, 3),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.ComputerChip, 1))
                ).WithCraftingTime(10.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("AutoFabricator");
            GameObject model = obj.transform.Find("Model").gameObject;

            GameObject inputContainer = obj.transform.Find("ContainerInput").gameObject;
            GameObject outputContainer = obj.transform.Find("ContainerOutput").gameObject;

            obj.AddComponent<BaseCrafter>();
            inputContainer.AddComponent<NetworkContainerRestriction>().Restrict(interfaceAllowed: false, requesterAllowed: true);
            outputContainer.AddComponent<NetworkContainerRestriction>().Restrict(interfaceAllowed: true, requesterAllowed: false);

            foreach (UniqueIdentifier uid in obj.GetComponentsInChildren<UniqueIdentifier>())
            {
                uid.classId = Info.ClassID;
            }

            ConstructableFlags constructableFlags = ConstructableFlags.Inside | ConstructableFlags.Wall;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }

    }
}
