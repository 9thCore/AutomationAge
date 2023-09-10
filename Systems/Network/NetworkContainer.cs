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
            NuclearReactor
        };

        public ContainerType Type { get; private set; } = ContainerType.None;

        internal StorageContainer storageContainer;
        internal BaseNuclearReactor nuclearReactor;

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
            Type = ContainerType.NuclearReactor;
            nuclearReactor = reactor;
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
                || Type == ContainerType.NuclearReactor;
        }

        public InventoryItem AddItem(Pickupable pickupable)
        {
            switch(Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.AddItem(pickupable);
                case ContainerType.NuclearReactor:
                    InventoryItem item = pickupable.inventoryItem;
                    if (nuclearReactor.equipment.GetFreeSlot(EquipmentType.NuclearReactor, out string slot))
                    {
                        nuclearReactor.equipment.AddItem(slot, item, forced: false);
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
                case ContainerType.NuclearReactor:
                    string slot = null;
                    foreach (KeyValuePair<string, InventoryItem> pair in nuclearReactor.equipment.equipment)
                    {
                        if (pair.Value.techType == techType)
                        {
                            slot = pair.Key;
                            break;
                        }
                    }

                    if(slot == null) { return null; }
                    return nuclearReactor.equipment.RemoveItem(slot, true, false).item;
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
                case ContainerType.NuclearReactor:
                    foreach (KeyValuePair<string, InventoryItem> pair in nuclearReactor.equipment.equipment)
                    {
                        if (pair.Value?.techType == item.techType) { return true; }
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
                case ContainerType.NuclearReactor:
                    return nuclearReactor.equipment.GetFreeSlot(EquipmentType.NuclearReactor, out string _);
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
                case ContainerType.NuclearReactor:
                    return nuclearReactor.IsAllowedToAdd(pickupable, false);
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
                case ContainerType.NuclearReactor:
                    return nuclearReactor.IsAllowedToRemove(pickupable, false);
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
                case ContainerType.NuclearReactor:
                    foreach(KeyValuePair<string, InventoryItem> pair in nuclearReactor.equipment.equipment)
                    {
                        if (pair.Value != null)
                        {
                            items.Add(pair.Value);
                        }
                    }
                    return items;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
