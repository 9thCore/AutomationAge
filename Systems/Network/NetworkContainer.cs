using ICSharpCode.SharpZipLib.Zip;
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
            NuclearReactor,
            BioReactor
        };

        public ContainerType Type { get; private set; } = ContainerType.None;

        internal StorageContainer storageContainer;
        internal BaseNuclearReactor nuclearReactor;
        internal BaseBioReactor bioReactor;

        public GameObject PrefabRoot = null;

        private BaseData _data;
        private BaseData Data => _data ??= PrefabRoot.transform.parent.gameObject.EnsureComponent<BaseData>();

        private NetworkContainerRestriction _restrictor;
        private NetworkContainerRestriction Restrictor => _restrictor ??= gameObject.GetComponent<NetworkContainerRestriction>();

        public bool interfaceAttached = false;
        public bool requesterAttached = false;
        public bool crafterAttached = false;
        public bool broadcasting = false;

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

        public void BioReactor(BaseBioReactor reactor)
        {
            Type = ContainerType.BioReactor;
            bioReactor = reactor;
        }

        public bool RequesterAllowed()
        {
            if (Restrictor == null) { return true; }
            return Restrictor.requesterAllowed;
        }

        public bool InterfaceAllowed()
        {
            if (Restrictor == null) { return true; }
            return Restrictor.interfaceAllowed;
        }

        public bool CrafterAllowed()
        {
            if (Restrictor == null) { return false; }
            return Restrictor.crafterAllowed;
        }

        public void Start()
        {
            if (PrefabRoot == null)
            {
                PrefabRoot = gameObject;
            }

            Data.AddContainer(this);

            switch (Type)
            {
                case ContainerType.StorageContainer:
                    storageContainer.container.onAddItem += OnAddItem;
                    storageContainer.container.onRemoveItem += OnRemoveItem;
                    break;
                case ContainerType.NuclearReactor:
                    nuclearReactor.equipment.onAddItem += OnAddItem;
                    nuclearReactor.equipment.onRemoveItem += OnRemoveItem;
                    break;
                case ContainerType.BioReactor:
                    bioReactor.container.onAddItem += OnAddItem;
                    bioReactor.container.onRemoveItem += OnRemoveItem;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnDestroy()
        {
            Data.RemoveContainer(this);
        }

        public void StartBroadcasting()
        {
            broadcasting = true;
        }

        public void StopBroadcasting()
        {
            broadcasting = false;
        }

        public bool IsAnythingAttached()
        {
            return interfaceAttached || requesterAttached || crafterAttached;
        }

        public bool ContainsItems()
        {
            return Type == ContainerType.StorageContainer
                || Type == ContainerType.NuclearReactor
                || Type == ContainerType.BioReactor;
        }

        public void OnAddItem(InventoryItem item)
        {
            Data.OnContainerAddItem(this, item);
        }

        public void OnRemoveItem(InventoryItem item)
        {
            Data.OnContainerRemoveItem(this, item);
        }

        public InventoryItem AddItem(Pickupable pickupable)
        {
            InventoryItem item = pickupable.inventoryItem;
            switch (Type)
            {
                case ContainerType.StorageContainer:
                    return storageContainer.container.AddItem(pickupable);
                case ContainerType.NuclearReactor:
                    if (nuclearReactor.equipment.GetFreeSlot(EquipmentType.NuclearReactor, out string slot))
                    {
                        nuclearReactor.equipment.AddItem(slot, item, forced: false);
                    }
                    return item;
                case ContainerType.BioReactor:
                    return bioReactor.container.AddItem(pickupable);
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
                        if (pair.Value?.techType == techType)
                        {
                            slot = pair.Key;
                            break;
                        }
                    }

                    if (slot == null) { return null; }
                    return nuclearReactor.equipment.RemoveItem(slot, true, false).item;
                case ContainerType.BioReactor:
                    return bioReactor.container.RemoveItem(techType);
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
                case ContainerType.BioReactor:
                    return bioReactor.container.Contains(item.techType);
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
                case ContainerType.BioReactor:
                    return bioReactor.container.HasRoomFor(pickupable);
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
                case ContainerType.BioReactor:
                    return ((IItemsContainer)bioReactor.container).AllowedToAdd(pickupable, false);
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
                case ContainerType.BioReactor:
                    return ((IItemsContainer)bioReactor.container).AllowedToRemove(pickupable, false);
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
                    foreach (KeyValuePair<string, InventoryItem> pair in nuclearReactor.equipment.equipment)
                    {
                        if (pair.Value != null)
                        {
                            items.Add(pair.Value);
                        }
                    }
                    return items;
                case ContainerType.BioReactor:
                    foreach (InventoryItem item in bioReactor.container)
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
