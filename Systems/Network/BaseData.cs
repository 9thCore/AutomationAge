using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class BaseData : MonoBehaviour
    {
        private bool updateQueued = false;

        public delegate void OnUpdate();
        public event OnUpdate onUpdate;

        public List<NetworkContainer> networkContainers = new List<NetworkContainer>();
        public List<InventoryItem> networkedItems = new List<InventoryItem>();

        public void UpdateItems()
        {
            updateQueued = true;
        }

        public void Update()
        {
            // Prefer to only update once per frame
            if (!updateQueued) { return; }
            updateQueued = false;

            networkedItems.Clear();

            foreach (NetworkContainer networkContainer in networkContainers)
            {
                if (!networkContainer.ContainsItems()) { continue; }
                List<InventoryItem> list = networkContainer.GetItems();
                networkedItems.AddRange(list);
            }

            onUpdate?.Invoke();
        }
    }
}
