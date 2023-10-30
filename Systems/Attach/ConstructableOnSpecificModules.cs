using System;
using HarmonyLib;
using UnityEngine;
using AutomationAge.Systems.Network;

namespace AutomationAge.Systems.Attach
{
    // Class that patches builder methods in order to only allow something to be constructable on other modules with specific requirements

    [HarmonyPatch]
    internal static class ConstructableOnSpecificModules
    {
        public const string DeconstructAttachedMessage = "DeconstructAttachedError";

        public static GameObject attachedModule = null;

        // Special handling required for some things like nuclear reactors
        public static bool IsSpecialModule(GameObject go, out GameObject matchingObject)
        {
            matchingObject = null;

            Transform tr = go.transform;
            while (tr.parent != null)
            {
                if (tr.gameObject.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                {
                    matchingObject = geometry.GetModule().gameObject;
                    return true;
                }
                else if (tr.gameObject.TryGetComponent(out BaseBioReactorGeometry geometry1))
                {
                    matchingObject = geometry1.GetModule().gameObject;
                    return true;
                }
                else if (tr.gameObject.TryGetComponent(out BaseFiltrationMachineGeometry geometry2))
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
            if (hitCollider == null) { return; }
            GameObject collider = hitCollider.gameObject;

            Attachable comp = Utility.GetComponentInHigherHierarchy<Attachable>(Builder.prefab);
            if (comp == null) { return; }

            if (comp.CanConstruct == null)
            {
                UnityEngine.Object.Destroy(comp);
                throw new InvalidOperationException($"Object {Builder.prefab} has the 'Attachable' component, but CanConstruct has not been assigned");
            }

            if (IsSpecialModule(collider, out GameObject root))
            {
                __result = comp.CanConstruct(root, out attachedModule);
                return;
            }

            root = UWE.Utils.GetEntityRoot(collider);

            if (!comp.AllowOnNonConstructables)
            {
                Constructable constructable = root.GetComponentInChildren<Constructable>();
                if (constructable == null)
                {
                    // Do not allow construction on non-constructables (this also includes base parts)
                    __result = false;
                    return;
                }

                if (constructable.constructedAmount < 1f)
                {
                    // Do not allow construction if the module is not fully constructed
                    __result = false;
                    return;
                }
            }

            __result = comp.CanConstruct(root, out attachedModule);
        }

        // Snap specific modules to others
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Builder), nameof(Builder.SetPlaceOnSurface))]
        public static void SetPlaceOnSurfacePostfix(ref RaycastHit hit, ref Vector3 position, ref Quaternion rotation)
        {
            if (hit.collider.gameObject == null) { return; }

            GameObject root = UWE.Utils.GetEntityRoot(hit.collider.gameObject);
            if (root == null) { return; }

            Attachable comp = Utility.GetComponentInHigherHierarchy<Attachable>(Builder.prefab);
            if (comp == null || comp.SnappingRule == null) { return; }

            comp.SnappingRule(root, ref position, ref rotation);
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
