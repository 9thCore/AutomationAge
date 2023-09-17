using AutomationAge.Buildables.Network.Items;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using AutomationAge.Systems.Network;
using AutomationAge.Systems.Miner;

namespace AutomationAge.Systems
{
    // Class that patches builder methods in order to only allow something to be constructable on other modules with specific requirements

    [HarmonyPatch]
    internal static class ConstructableOnSpecificModules
    {
        public class SpecialRuleObject
        {
            public Vector3 position;
            public Quaternion rotation;
            public bool changed = false;

            public SpecialRuleObject(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }

        private const string DeconstructAttachedMessage = "DeconstructAttachedError";

        private static Dictionary<TechType, Func<GameObject, bool>> specialConstructables = new Dictionary<TechType, Func<GameObject, bool>>()
        {
            {
                ItemInterface.Info.TechType,
                obj => {
                    if(obj.TryGetComponent(out NetworkContainer container))
                    {
                        return !container.interfaceAttached && container.interfaceAllowed;
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
                        return !container.requesterAttached && container.requesterAllowed;
                    }

                    return false;
                }
            },
            {
                RockDriller.Info.TechType,
                obj =>
                {
                    if(obj.TryGetComponent(out BaseMiner miner))
                    {
                        return !miner.hasDrillAttachment;
                    }

                    return false;
                }
            }
        };

        // Describes special construction rules, used for snapping a construction to another for instance
        private static readonly Dictionary<TechType, Action<GameObject, SpecialRuleObject>> specialConstructionRules = new()
        {
            {
                RockDriller.Info.TechType,
                (go, obj) =>
                {
                    if (!go.TryGetComponent(out BaseMiner _)) { return; }

                    obj.position = go.transform.position + go.transform.up * 2f;
                    obj.rotation = go.transform.rotation;
                    obj.changed = true;
                }
            }
        };

        public static GameObject attachedModule = null;

        // Special handling required for some things like nuclear reactors
        public static bool IsSpecialModule(GameObject go, out GameObject matchingObject)
        {
            matchingObject = null;

            Transform tr = go.transform;
            while(tr.parent != null)
            {
                if (tr.gameObject.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                {
                    matchingObject = geometry.GetModule().gameObject;
                    return true;
                } else if (tr.gameObject.TryGetComponent(out BaseBioReactorGeometry geometry1))
                {
                    matchingObject = geometry1.GetModule().gameObject;
                    return true;
                } else if (tr.gameObject.TryGetComponent(out BaseFiltrationMachineGeometry geometry2))
                {
                    matchingObject = geometry2.GetModule().gameObject;
                    return true;
                }

                tr = tr.parent;
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

            Transform tr = hitCollider.transform;

            if (IsSpecialModule(tr.gameObject, out GameObject obj)) {
                attachedModule = obj;
                __result = func(obj);
                return;
            }

            while (tr != null)
            {
                if (!tr.gameObject.TryGetComponent(out Constructable constructable))
                {
                    // Do not allow construction on non-constructables (this also includes base parts)
                    // It is possible we are in a 'colliders' GameObject though, so keep looking upwards
                    __result = false;
                    tr = tr.parent;
                    continue;
                }

                if (constructable.constructedAmount < 1f)
                {
                    // Do not allow construction if the module is not fully constructed
                    __result = false;
                    return;
                }

                attachedModule = tr.gameObject;
                __result = func(tr.gameObject);
                return;
            }
        }

        // Snap specific modules to others
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Builder), nameof(Builder.SetPlaceOnSurface))]
        public static void SetPlaceOnSurfacePostfix(ref RaycastHit hit, ref Vector3 position, ref Quaternion rotation)
        {
            if (!hit.collider.gameObject) { return; }
            Transform tr = hit.collider.transform;

            TechType type = Builder.lastTechType;
            if (specialConstructionRules.TryGetValue(type, out Action<GameObject, SpecialRuleObject> action))
            {
                SpecialRuleObject obj = new SpecialRuleObject(position, rotation);

                while (tr != null)
                {
                    action(tr.gameObject, obj);
                    if (obj.changed)
                    {
                        position = obj.position;
                        rotation = obj.rotation;
                        return;
                    }

                    tr = tr.parent;
                }
            }
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
                reason = Language.main.Get(DeconstructAttachedMessage);
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
            if (IsSpecialModule(__instance.gameObject, out GameObject obj))
            {
                go = obj;
            }
            if (go == null) { return; }

            if (go.TryGetComponent(out NetworkContainer container))
            {
                __result = !container.IsAnythingAttached();
                reason = Language.main.Get(DeconstructAttachedMessage);
                return;
            }

            return;
        }
    }
}
