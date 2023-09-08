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
        public ContainerType type = ContainerType.None;
        public StorageContainer storageContainer;

        public bool interfaceAttached = false;
        public bool requesterAttached = false;
        private bool broadcasting = false;

        public void StorageContainer(StorageContainer container)
        {
            type = ContainerType.StorageContainer;
            storageContainer = container;

            storageContainer.container.onAddItem += UpdateItems;
            storageContainer.container.onRemoveItem += UpdateItems;
        }

        public void StartBroadcasting()
        {
            if (broadcasting) { return; }
            broadcasting = true;

            GetBaseData().networkContainers.Add(this);
            UpdateItems();
        }

        public void StopBroadcasting()
        {
            if (!broadcasting) { return; }
            broadcasting = false;

            GetBaseData().networkContainers.Remove(this);
            UpdateItems();
        }

        public bool IsAnythingAttached()
        {
            return interfaceAttached || requesterAttached;
        }

        public bool ContainsItems()
        {
            return type == ContainerType.StorageContainer;
        }

        public InventoryItem AddItem(Pickupable pickupable)
        {
            switch(type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.AddItem(pickupable);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public Pickupable RemoveItem(TechType techType)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.RemoveItem(techType);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public bool Contains(InventoryItem item)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.Contains(item.techType);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public bool HasRoomFor(Pickupable pickupable)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.HasRoomFor(pickupable);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public bool AllowedToAdd(Pickupable pickupable)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return ((IItemsContainer)storageContainer.container).AllowedToAdd(pickupable, false);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public bool AllowedToRemove(Pickupable pickupable)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return ((IItemsContainer)storageContainer.container).AllowedToRemove(pickupable, false);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public List<InventoryItem> GetItems()
        {
            switch(type)
            {
                case ContainerType.StorageContainer:
                    List<InventoryItem> items = new List<InventoryItem>();
                    foreach (InventoryItem item in storageContainer.container)
                    {
                        items.Add(item);
                    }
                    return items;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private BaseData GetBaseData()
        {
            GameObject baseRoot = transform.parent.gameObject;
            return baseRoot.EnsureComponent<BaseData>();
        }

        // Required for onAddItem/onRemoveItem
        // Although this is a good place to check if we're broadcasting
        private void UpdateItems(InventoryItem _)
        {
            if (!broadcasting) { return; }
            UpdateItems();
        }

        private void UpdateItems()
        {
            GetBaseData().UpdateItems();
        }

        public void OnDestroy()
        {
            if(storageContainer?.container == null) { return; }
            storageContainer.container.onAddItem -= UpdateItems;
            storageContainer.container.onRemoveItem -= UpdateItems;
        }
    }
}
