using static AutomationAge.Systems.Attach.AttachableModule;
using UnityEngine;
using AutomationAge.Systems.Blueprint;
using AutomationAge.Systems.AutoCrafting;
using Nautilus.Handlers;
using System;

namespace AutomationAge.Systems
{
    internal static class Utility
    {
        private static Vector3 DefaultSearchRadius = new Vector3(1f, 1f, 1f);

        // Attempts to find an object based on its position and PrefabIdentifier id
        // Optional Vector3 and SpecialModule parameters to further customize search
        public static bool FindObject(out GameObject go, Vector3 position, string prefabIdentifierID, Vector3 searchBoxDimensions, SpecialModule module = SpecialModule.None)
        {
            Collider[] colliders = Physics.OverlapBox(position, searchBoxDimensions);

            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject obj = colliders[i].gameObject;
                PrefabIdentifier id = null;

                switch(module)
                {
                    case SpecialModule.None:
                        id = GetComponentInHigherHierarchy<PrefabIdentifier>(obj);
                        break;
                    case SpecialModule.NuclearReactor:
                        BaseNuclearReactorGeometry geometry = GetComponentInHigherHierarchy<BaseNuclearReactorGeometry>(obj);
                        if (geometry != null)
                        {
                            id = GetComponentInHigherHierarchy<PrefabIdentifier>(geometry.GetModule().gameObject);
                        }
                        break;
                    case SpecialModule.BioReactor:
                        BaseBioReactorGeometry geometry1 = GetComponentInHigherHierarchy<BaseBioReactorGeometry>(obj);
                        if (geometry1 != null)
                        {
                            id = GetComponentInHigherHierarchy<PrefabIdentifier>(geometry1.GetModule().gameObject);
                        }
                        break;
                    case SpecialModule.WaterPark:
                        WaterParkGeometry geometry2 = GetComponentInHigherHierarchy<WaterParkGeometry>(obj);
                        if (geometry2 != null)
                        {
                            id = GetComponentInHigherHierarchy<PrefabIdentifier>(geometry2.GetModule().gameObject);
                        }
                        break;
                    default:
                        throw new ArgumentException();
                }

                if (id != null && id.Id == prefabIdentifierID)
                {
                    go = id.gameObject;
                    return true;
                }
            }

            go = null;
            return false;
        }

        public static bool FindObject(out GameObject go, Vector3 position, string prefabIdentifierID, SpecialModule module = SpecialModule.None)
        {
            return FindObject(out go, position, prefabIdentifierID, DefaultSearchRadius, module);
        }

        public static T GetComponentInHigherHierarchy<T>(GameObject obj)
        {
            if (obj.TryGetComponent(out T component))
            {
                return component;
            }
            return obj.GetComponentInParent<T>();
        }

        public static int SnapRotationToCardinal(float degrees, int offset = 0)
        {
            return (int)degrees * 90 + offset;
        }
    }
}
