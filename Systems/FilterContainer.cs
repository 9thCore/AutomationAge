using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems
{
    internal class FilterContainer
    {
        private ItemsContainer _container;
        public ItemsContainer container
        {
            get
            {
                if (_container == null)
                {
                    Attach();
                }
                return _container;
            }
        }

        private GameObject obj;

        public FilterContainer(GameObject go)
        {
            obj = go;
        }

        public void Attach()
        {
            if (!obj.TryGetComponent(out StorageContainer storageContainer))
            {
                throw new InvalidOperationException("missing StorageContainer component");
            }

            _container = storageContainer.container;
            _container.isAllowedToAdd += AllowedToAdd;
        }

        public List<InventoryItem> GetItems()
        {
            List<InventoryItem> items = new List<InventoryItem>();
            foreach (KeyValuePair<TechType, ItemsContainer.ItemGroup> pair in container._items)
            {
                foreach(InventoryItem item in pair.Value.items)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        public void OnDestroy()
        {
            if (_container == null) { return; }
            container.isAllowedToAdd -= AllowedToAdd;
        }

        public bool AllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return !container.Contains(pickupable.GetTechType());
        }
    }
}
