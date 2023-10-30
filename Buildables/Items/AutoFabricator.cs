using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems;
using AutomationAge.Systems.AutoCrafting;
using AutomationAge.Systems.Network;
using AutomationAge.Systems.Attach;

namespace AutomationAge.Buildables.Items
{
    internal class AutoFabricator
    {
        public static string InputContainerName = "ContainerInput";

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
                    new Ingredient(TechType.ComputerChip, 1)))
                .WithCraftingTime(10.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("AutoFabricator");
            GameObject model = obj.transform.Find("Model").gameObject;

            GameObject inputContainer = obj.transform.Find(InputContainerName).gameObject;

            obj.AddComponent<AutoCrafter>();
            inputContainer.AddComponent<NetworkContainerRestriction>().Restrict(interfaceAllowed: false, requesterAllowed: true);
            NetworkContainer c = inputContainer.AddComponent<NetworkContainer>();
            c.PrefabRoot = obj;

            Attachable check = obj.AddComponent<Attachable>();
            check.CanConstruct = (GameObject input, out GameObject module) =>
            {
                NetworkContainer container = input.GetComponentInChildren<NetworkContainer>();
                if (container != null && container.CrafterAllowed() && !container.crafterAttached)
                {
                    module = container.gameObject;
                    return true;
                }

                module = null;
                return false;
            };

            check.SnappingRule = (GameObject input, ref Vector3 position, ref Quaternion rotation) =>
            {
                NetworkContainer container = input.GetComponentInChildren<NetworkContainer>();
                if (container == null)
                {
                    return false;
                }

                position = container.transform.position + container.transform.up * 1f + container.transform.forward * 0.25f;
                rotation = container.transform.rotation;
                return true;
            };

            foreach (UniqueIdentifier uid in obj.GetComponentsInChildren<UniqueIdentifier>())
            {
                uid.classId = Info.ClassID;
            }

            ConstructableFlags constructableFlags = ConstructableFlags.Inside | ConstructableFlags.Wall;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Global);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }

    }
}
