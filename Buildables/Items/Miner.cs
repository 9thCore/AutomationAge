using AutomationAge.Systems;
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems.Miner;

namespace AutomationAge.Buildables.Items
{
    internal class Miner
    {
        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("Miner", "Rock Extruder", "When built on the ground, consumes power in order to extrude rocks and minerals present in its current biome.")
            .WithIcon(SpriteManager.Get(TechType.Nickel));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.ExteriorModules, TechCategory.ExteriorModule);
            prefab.SetRecipe(
                new RecipeData(
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.WiringKit, 1)))
                .WithCraftingTime(10.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject("Miner");
            GameObject model = obj.transform.Find("model/MinerBodyModel").gameObject;

            obj.AddComponent<BaseMiner>();

            ConstructableFlags constructableFlags = ConstructableFlags.Outside | ConstructableFlags.Ground | ConstructableFlags.Rotatable;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }
    }
}
