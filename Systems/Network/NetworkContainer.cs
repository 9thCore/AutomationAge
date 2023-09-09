﻿using System;
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

        private BaseData data;
        public bool interfaceAttached = false;
        public bool requesterAttached = false;
        private bool broadcasting = false;

        public void StorageContainer(StorageContainer container)
        {
            type = ContainerType.StorageContainer;
            storageContainer = container;

            GameObject baseRoot = transform.parent.gameObject;
            data = baseRoot.EnsureComponent<BaseData>();
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
    }
}
