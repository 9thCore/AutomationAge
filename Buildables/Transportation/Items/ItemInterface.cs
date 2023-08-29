using Nautilus.Assets.PrefabTemplates;
using Nautilus.Assets;
using Nautilus.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CraftData;
using Nautilus.Utility;
using Nautilus.Assets.Gadgets;
using UnityEngine;
using UnityEngine.UI;

namespace AutomationAge.Buildables.Transportation.Items
{
    internal static class ItemInterface
    {
        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("ItemInterface", "Item Interface", "When attached to a storage module, will expose its contents which can be requested by Item Requesters.")
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
            GameObject obj = Assets.GetGameObject("ItemInterface");
            GameObject model = obj.transform.Find("ItemInterfaceModel").gameObject;

            ConstructableFlags constructableFlags = ConstructableFlags.AllowedOnConstructable | ConstructableFlags.Base | ConstructableFlags.Wall;
            PrefabUtils.AddBasicComponents(obj, "ItemInterface", Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }
    }
}
