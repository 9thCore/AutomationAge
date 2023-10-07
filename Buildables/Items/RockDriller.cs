﻿using AutomationAge.Systems;
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Utility;
using static CraftData;
using UnityEngine;
using Nautilus.Assets.Gadgets;
using AutomationAge.Systems.Miner;

namespace AutomationAge.Buildables.Items
{
    internal class RockDriller
    {
        public const string MainObject = "RockDriller";
        public const string ModelObject = "RockDrillerModel";
        public const string StorageRootObject = "StorageRoot";
        public const string ContainerObject = "Container";

        public const string StorageRoot = "RockDrillerRoot";
        public const string StorageRootClassID = "RockDrillerStorage";
        public const int Width = 4;
        public const int Height = 4;

        public static PrefabInfo Info { get; } = PrefabInfo.WithTechType("RockDriller", "Rock Driller", "Attachment for the Rock Extruder. Automatically breaks down outcrops and picks up raw resources.")
            .WithIcon(SpriteManager.Get(TechType.SmallLocker));

        public static void Register()
        {
            CustomPrefab prefab = new CustomPrefab(Info);

            prefab.SetGameObject(GetGameObject);
            prefab.SetPdaGroupCategory(TechGroup.ExteriorModules, TechCategory.ExteriorModule);
            prefab.SetRecipe(new RecipeData(new Ingredient(TechType.Titanium, 8), new Ingredient(TechType.AdvancedWiringKit, 1))).WithCraftingTime(5.0f);
            prefab.Register();
        }

        public static GameObject GetGameObject()
        {
            GameObject obj = Assets.GetGameObject(MainObject);
            GameObject model = obj.transform.Find(ModelObject).gameObject;

            obj.AddComponent<Driller>();

            foreach (UniqueIdentifier uid in obj.GetComponentsInChildren<UniqueIdentifier>())
            {
                uid.classId = Info.ClassID;
            }

            ConstructableFlags constructableFlags = ConstructableFlags.Outside | ConstructableFlags.AllowedOnConstructable | ConstructableFlags.Rotatable;
            PrefabUtils.AddBasicComponents(obj, Info.ClassID, Info.TechType, LargeWorldEntity.CellLevel.Near);
            PrefabUtils.AddConstructable(obj, Info.TechType, constructableFlags, model);
            MaterialUtils.ApplySNShaders(model);

            return obj;
        }
    }
}