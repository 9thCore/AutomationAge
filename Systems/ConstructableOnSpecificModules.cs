using AutomationAge.Buildables.Network.Items;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using AutomationAge.Systems.Network;

namespace AutomationAge.Systems
{
    // Class that patches builder methods in order to only allow something to be constructable on other modules with specific requirements

    [HarmonyPatch]
    internal static class ConstructableOnSpecificModules
    {
        private const string deconstructAttachedMessage = "DeconstructAttachedError";

        private static Dictionary<TechType, Func<GameObject, bool>> specialConstructables = new Dictionary<TechType, Func<GameObject, bool>>()
        {
            {
                ItemInterface.Info.TechType,
                obj => {
                    if(obj.TryGetComponent(out NetworkContainer container))
                    {
                        return !container.interfaceAttached;
                    }

                    return false;
                }
            },
            {
                ItemRequester.Info.TechType,
                obj =>
                {
                    if(obj.TryGetComponent(out NetworkContainer container))
                    {
                        return !container.requesterAttached;
                    }

                    return false;
                }
            }
        };

        public static GameObject attachedModule = null;

        // Allow constructing on modules if they fit the specified criteria
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Builder), nameof(Builder.CheckAsSubModule))]
        public static void CheckAsSubModulePostfix(Collider hitCollider, ref bool __result)
        {
            if (!__result) { return; }

            TechType type = Builder.lastTechType;
            if (!specialConstructables.ContainsKey(type)) { return; }

            GameObject module = hitCollider.transform.parent.gameObject;

            if(!module.TryGetComponent(out Constructable constructable)) {
                // Do not allow construction on non-constructables (this also includes base parts)
                __result = false;
                return;
            }

            if(constructable.constructedAmount < 1f)
            {
                // Do not allow construction if the module is not fully constructed
                __result = false;
                return;
            }

            attachedModule = module;
            __result = specialConstructables[type](module);
        }

        // Disallow deconstruction of module if it has something attached to it
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Constructable), nameof(Constructable.DeconstructionAllowed))]
        public static bool DeconstructionAllowedPrefix(Constructable __instance, ref bool __result, ref string reason)
        {
            GameObject obj = __instance.gameObject;
            if (obj.TryGetComponent(out NetworkContainer container))
            {
                __result = !container.IsAnythingAttached();
                reason = Language.main.Get(deconstructAttachedMessage);
                return false;
            }

            return true;
        }

        // Remove "Press [KEY] to deconstruct" text if the module has something attached to it
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.OnHover), typeof(Constructable))]
        public static bool OnHoverPrefix(Constructable constructable)
        {
            GameObject obj = constructable.gameObject;
            if (obj.TryGetComponent(out NetworkContainer container))
            {
                return !container.IsAnythingAttached();
            }

            return true;
        }
    }
}
