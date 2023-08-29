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
        [HarmonyPatch(typeof(Builder), nameof(Builder.UpdateAllowed))]
        public static void Postfix(ref bool __result)
        {
            if(!__result) { return; }

            TechType type = Builder.lastTechType;
            if(!specialConstructables.ContainsKey(type)) { return; }

            GameObject hitObj = null;
            Vector3 hitPosition = default;
            UWE.Utils.TraceForTarget(Builder.placePosition, Camera.main.transform.forward, Builder.prefab, Builder.placeMaxDistance, ref hitObj, ref hitPosition);
            if(hitObj == null) { return; }

            __result = specialConstructables[type](hitObj);
        }
    }
}
