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
            StorageContainer
        };
        public ContainerType Type { get; private set; } = ContainerType.None;

        public StorageContainer storageContainer;

        private BaseData _data;
        private BaseData data => _data ??= transform.parent.gameObject.EnsureComponent<BaseData>();

        public bool interfaceAttached = false;
        public bool requesterAttached = false;
        private bool broadcasting = false;

        public void StorageContainer(StorageContainer container)
        {
            Type = ContainerType.StorageContainer;
            storageContainer = container;
        }

        public void StartBroadcasting()
        {
            if (broadcasting) { return; }
            broadcasting = true;

            data.networkContainers.Add(this);
        }

        public void StopBroadcasting()
        {
            if (!broadcasting) { return; }
            broadcasting = false;

            data.networkContainers.Remove(this);
        }

        public bool IsAnythingAttached()
        {
            return interfaceAttached || requesterAttached;
        }

        public bool ContainsItems()
        {
            return Type == ContainerType.StorageContainer;
        }

        public InventoryItem AddItem(Pickupable pickupable)
        {
            switch(Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.AddItem(pickupable);
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<InventoryItem> GetItems()
        {
            switch(Type)
            {
                case ContainerType.StorageContainer:
                    List<InventoryItem> items = new List<InventoryItem>();
                    foreach (InventoryItem item in storageContainer.container)
                    {
                        items.Add(item);
                    }
                    return items;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
