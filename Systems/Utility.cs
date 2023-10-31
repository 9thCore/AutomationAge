﻿using static AutomationAge.Systems.Attach.AttachableModule;
using UnityEngine;
using AutomationAge.Systems.Blueprint;
using AutomationAge.Systems.AutoCrafting;
using Nautilus.Handlers;

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
                Transform tr = colliders[i].transform;
                while (tr != null)
                {
                    go = tr.gameObject;

                    switch (module)
                    {
                        case SpecialModule.NuclearReactor:
                            GameObject obj = go.transform.parent.gameObject;
                            if (obj.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                            {
                                go = geometry.GetModule().gameObject;
                            }
                            break;
                        case SpecialModule.BioReactor:
                            GameObject obj1 = go.transform.parent.gameObject;
                            if (obj1.TryGetComponent(out BaseBioReactorGeometry geometry1))
                            {
                                go = geometry1.GetModule().gameObject;
                            }
                            break;
                        default:
                            break;
                    }

                    if (go.TryGetComponent(out PrefabIdentifier identifier) && identifier.Id == prefabIdentifierID)
                    {
                        return true;
                    }

                    tr = tr.parent;
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

        public static GameObject CreateEquipmentSlot(GameObject slotClone, string name, Vector3 position = default)
        {
            GameObject equipmentSlot = Object.Instantiate(slotClone, slotClone.transform.parent);
            equipmentSlot.name = name;
            equipmentSlot.transform.localPosition = position;
            equipmentSlot.GetComponent<uGUI_EquipmentSlot>().slot = name;

            return equipmentSlot;
        }

        public static void CreateEquipmentSlots(GameObject slotClone)
        {
            BlueprintEncoder.CreateEquipmentSlots(slotClone);
            AutoCrafter.CreateEquipmentSlots(slotClone);
        }

        public static EquipmentType CreateEquipmentType(string name)
        {
            EquipmentType type = EnumHandler.AddEntry<EquipmentType>(name);
            MapEquipmentType(name, type);
            return type;
        }

        public static void MapEquipmentType(string name, EquipmentType type)
        {
            Equipment.slotMapping.Add(name, type);
        }
    }
}
