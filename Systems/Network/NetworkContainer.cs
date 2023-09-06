using System;
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
        }

        public void StartBroadcasting()
        {
            if (broadcasting) { return; }
            broadcasting = true;

            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();
            data.networkContainers.Add(this);
        }

        public void StopBroadcasting()
        {
            if (!broadcasting) { return; }
            broadcasting = false;

            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();
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

        public bool RemoveItem(Pickupable pickupable)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.RemoveItem(pickupable);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public bool Contains(InventoryItem item)
        {
            switch (type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.Contains(item);
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
    }
}
