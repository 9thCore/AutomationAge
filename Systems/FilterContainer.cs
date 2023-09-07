using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;
using static CraftNode;

namespace AutomationAge.Systems
{
    internal class FilterContainer
    {
        public ItemsContainer container { get; private set; }

        public void QueueAttach(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out StorageContainer storageContainer))
            {
                throw new InvalidOperationException("missing StorageContainer component");
            }

            CoroutineHost.StartCoroutine(Attach(storageContainer));
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

        private IEnumerator Attach(StorageContainer storageContainer)
        {
            yield return new WaitUntil(() => storageContainer.container != null);

            container = storageContainer.container;
            container.isAllowedToAdd += (Pickupable pickupable, bool verbose) =>
            {
                return !container.Contains(pickupable.GetTechType());
            };
        }
    }
}
