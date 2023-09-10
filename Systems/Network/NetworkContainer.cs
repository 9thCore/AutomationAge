using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkContainer : MonoBehaviour
    {
        public enum ContainerType
        {
            None,
            StorageContainer,
            Equipment
        };
        public enum EquipmentSubtype
        {
            None,
            NuclearReactor
        };

        public ContainerType Type { get; private set; } = ContainerType.None;
        public EquipmentSubtype SubType { get; private set; } = EquipmentSubtype.None;

        public StorageContainer storageContainer;
        public BaseNuclearReactor nuclearReactor;
        public Equipment equipment;
        public EquipmentType equipmentType;

        private BaseData _data;
        private BaseData Data => _data ??= transform.parent.gameObject.EnsureComponent<BaseData>();

        public bool interfaceAttached = false;
        public bool requesterAttached = false;
        private bool broadcasting = false;

        public void StorageContainer(StorageContainer container)
        {
            Type = ContainerType.StorageContainer;
            storageContainer = container;
        }

        public void NuclearReactor(BaseNuclearReactor reactor)
        {
            Type = ContainerType.Equipment;
            SubType = EquipmentSubtype.NuclearReactor;
            nuclearReactor = reactor;
            equipment = reactor.equipment;
            equipmentType = EquipmentType.NuclearReactor;
        }

        public void StartBroadcasting()
        {
            if (broadcasting) { return; }
            broadcasting = true;

            Data.networkContainers.Add(this);
        }

        public void StopBroadcasting()
        {
            if (!broadcasting) { return; }
            broadcasting = false;

            Data.networkContainers.Remove(this);
        }

        public bool IsAnythingAttached()
        {
            return interfaceAttached || requesterAttached;
        }

        public bool ContainsItems()
        {
            return Type == ContainerType.StorageContainer
                || Type == ContainerType.Equipment;
        }

        public InventoryItem AddItem(Pickupable pickupable)
        {
            switch(Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.AddItem(pickupable);
                case ContainerType.Equipment:
                    InventoryItem item = pickupable.inventoryItem;
                    if (equipment.GetFreeSlot(equipmentType, out string slot))
                    {
                        Plugin.Logger.LogInfo($"Adding to reactor {nuclearReactor.name}, slot {slot} | {equipment.AddItem(slot, item, forced: false)} | Item container is now {item.container.label}");
                    }
                    return item;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Pickupable RemoveItem(TechType techType)
        {
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.RemoveItem(techType);
                case ContainerType.Equipment:
                    string slot = null;
                    foreach (KeyValuePair<string, InventoryItem> pair in equipment.equipment)
                    {
                        if (pair.Value.techType == techType)
                        {
                            slot = pair.Key;
                            break;
                        }
                    }

                    if(slot == null) { return null; }
                    return equipment.RemoveItem(slot, true, false).item;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Contains(InventoryItem item)
        {
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.Contains(item.techType);
                case ContainerType.Equipment:
                    foreach (KeyValuePair<string, InventoryItem> pair in equipment.equipment)
                    {
                        if (pair.Value.techType == item.techType) { return true; }
                    }
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool HasRoomFor(Pickupable pickupable)
        {
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.HasRoomFor(pickupable);
                case ContainerType.Equipment:
                    return equipment.GetFreeSlot(equipmentType, out string _);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool AllowedToAdd(Pickupable pickupable)
        {
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return ((IItemsContainer)storageContainer.container).AllowedToAdd(pickupable, false);
                case ContainerType.Equipment:
                    return SubType switch
                    {
                        EquipmentSubtype.NuclearReactor => nuclearReactor.IsAllowedToAdd(pickupable, false),
                        _ => false
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool AllowedToRemove(Pickupable pickupable)
        {
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return ((IItemsContainer)storageContainer.container).AllowedToRemove(pickupable, false);
                case ContainerType.Equipment:
                    return SubType switch
                    {
                        EquipmentSubtype.NuclearReactor => nuclearReactor.IsAllowedToRemove(pickupable, false),
                        _ => false
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<InventoryItem> GetItems()
        {
            List<InventoryItem> items = new List<InventoryItem>();
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    foreach (InventoryItem item in storageContainer.container)
                    {
                        items.Add(item);
                    }
                    return items;
                case ContainerType.Equipment:
                    foreach(KeyValuePair<string, InventoryItem> pair in equipment.equipment)
                    {
                        items.Add(pair.Value);
                    }
                    return items;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
