using Nautilus.Handlers;
using UnityEngine;

namespace AutomationAge.Systems
{
    internal static class EquipmentHandler
    {
        public delegate void EquipmentEvent();
        public static EquipmentEvent InitEquipment;

        public static GameObject slotClone;

        public static void CreateEquipmentSlots(GameObject clone)
        {
            slotClone = clone;
            InitEquipment?.Invoke();
        }

        public static GameObject CreateEquipmentSlot(string name, Vector3 position = default)
        {
            GameObject equipmentSlot = Object.Instantiate(slotClone, slotClone.transform.parent);
            equipmentSlot.name = name;
            equipmentSlot.transform.localPosition = position;
            equipmentSlot.GetComponent<uGUI_EquipmentSlot>().slot = name;

            return equipmentSlot;
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
