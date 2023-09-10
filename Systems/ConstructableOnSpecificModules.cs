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

        // Special handling required for some things like nuclear reactors
        public static bool IsSpecialModule(GameObject go, out GameObject matchingObject, out BaseNuclearReactor reactor, out BaseBioReactor bioReactor)
        {
            reactor = null;
            bioReactor = null;
            matchingObject = null;

            GameObject obj = go;
            while(true)
            {
                if (obj.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                {
                    reactor = geometry.GetModule();
                    matchingObject = reactor.gameObject;
                    return true;
                } else if (obj.TryGetComponent(out BaseBioReactorGeometry geometry1))
                {
                    bioReactor = geometry1.GetModule();
                    matchingObject = bioReactor.gameObject;
                    return true;
                }

                if (obj.transform.parent == null) { break; }
                obj = obj.transform.parent.gameObject;
            }

            return false;
        }

        // Allow constructing on modules if they fit the specified criteria
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Builder), nameof(Builder.CheckAsSubModule))]
        public static void CheckAsSubModulePostfix(Collider hitCollider, ref bool __result)
        {
            if (hitCollider == null || hitCollider.transform.parent == null) { return; }

            TechType type = Builder.lastTechType;
            if (!specialConstructables.ContainsKey(type)) { return; }
            Func<GameObject, bool> func = specialConstructables[type];

            GameObject go = hitCollider.transform.parent.gameObject;

            if (IsSpecialModule(go, out GameObject obj, out BaseNuclearReactor _, out BaseBioReactor _)) {
                attachedModule = obj;
                __result = func(obj);
                return;
            }

            if(!go.TryGetComponent(out Constructable constructable)) {
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

            attachedModule = go;
            __result = func(go);
        }

        // Disallow deconstruction of module if it has something attached to it
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Constructable), nameof(Constructable.DeconstructionAllowed))]
        public static bool ConstructableDeconstructionAllowed(Constructable __instance, ref bool __result, ref string reason)
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseDeconstructable), nameof(BaseDeconstructable.DeconstructionAllowed))]
        public static void DeconstructableDeconstructionAllowed(BaseDeconstructable __instance, ref bool __result, ref string reason)
        {
            GameObject go = null;
            if (IsSpecialModule(__instance.gameObject, out GameObject obj, out BaseNuclearReactor _, out BaseBioReactor _))
            {
                go = obj;
            }
            if (go == null) { return; }

            if (go.TryGetComponent(out NetworkContainer container))
            {
                __result = !container.IsAnythingAttached();
                reason = Language.main.Get(deconstructAttachedMessage);
                return;
            }

            return;
        }
    }
}
