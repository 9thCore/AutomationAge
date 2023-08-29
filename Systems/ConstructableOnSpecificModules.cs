using AutomationAge.Buildables.Transportation.Items;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AutomationAge.Systems
{
    // Class that patches builder methods in order to only allow something to be constructable on other modules with specific requirements

    [HarmonyPatch]
    internal static class ConstructableOnSpecificModules
    {
        private static Dictionary<TechType, Func<GameObject, bool>> specialConstructables = new Dictionary<TechType, Func<GameObject, bool>>()
        {
            { ItemInterface.Info.TechType, obj => obj.GetComponent<StorageContainer>() != null }
        };

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Builder), nameof(Builder.CheckAsSubModule))]
        public static void Postfix(Collider hitCollider, ref bool __result)
        {
            if(hitCollider == null) { return; }
            if (!__result) { return; }

            TechType type = Builder.lastTechType;
            if (!specialConstructables.ContainsKey(type)) { return; }

            GameObject module = hitCollider.transform.parent.gameObject;
            __result = specialConstructables[type](module);
        }
    }
}
