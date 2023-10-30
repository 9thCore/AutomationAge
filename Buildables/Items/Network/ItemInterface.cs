using Nautilus.Assets;
using Nautilus.Crafting;
using static CraftData;
using Nautilus.Utility;
using Nautilus.Assets.Gadgets;
using UnityEngine;
using AutomationAge.Systems;
using AutomationAge.Systems.Network;
using AutomationAge.Systems.Attach;
using System;

namespace AutomationAge.Buildables.Items.Network
{
    internal static class ItemInterface
    {
        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("ItemInterface", "Item Interface", "When attached to a storage module, will expose its contents which can be requested by Item Requesters. Continually consumes power.")
            .WithIcon(SpriteManager.Get(TechType.Locker));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            prefab.SetRecipe(
                new RecipeData(
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.WiringKit, 1)))
                .WithCraftingTime(2.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("ItemInterface");
            GameObject model = obj.transform.Find("ItemInterfaceModel").gameObject;

            obj.AddComponent<PowerConsumer>();
            obj.AddComponent<NetworkInterface>();

            Attachable check = obj.AddComponent<Attachable>();
            check.CanConstruct = (GameObject input, out GameObject module) =>
            {
                NetworkContainer container = input.GetComponentInChildren<NetworkContainer>();
                if (container != null && container.InterfaceAllowed() && !container.interfaceAttached)
                {
                    module = container.gameObject;
                    return true;
                }

                module = null;
                return false;
            };

            ConstructableFlags constructableFlags = ConstructableFlags.AllowedOnConstructable | ConstructableFlags.Base | ConstructableFlags.Wall;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }
    }
}
