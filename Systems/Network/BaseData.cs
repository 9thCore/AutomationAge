using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class BaseData : MonoBehaviour
    {
        private Dictionary<TechType, List<NetworkContainer>> networkContainersWithItem = new Dictionary<TechType, List<NetworkContainer>>();
        public List<NetworkContainer> networkContainers = new List<NetworkContainer>();

        private List<NetworkContainer> GetBroadcastingContainers(List<NetworkContainer> containers)
        {
            List<NetworkContainer> broadcastingContainers = new List<NetworkContainer>();
            foreach (NetworkContainer container in containers)
            {
                if (container.broadcasting) { broadcastingContainers.Add(container); }
            }

            return broadcastingContainers;
        }

        public List<NetworkContainer> GetContainersContaining(TechType type, bool sanitize = true)
        {
            if (networkContainersWithItem.TryGetValue(type, out List<NetworkContainer> containers))
            {
                return sanitize ? GetBroadcastingContainers(containers) : containers;
            }
            return null;
        }

        public void AddContainer(NetworkContainer container)
        {
            networkContainers.Add(container);

            if (container.ContainsItems())
            {
                List<InventoryItem> items = container.GetItems();
                foreach (InventoryItem item in items)
                {
                    List<NetworkContainer> containers = GetContainersContaining(item.techType, false);
                    if (containers == null)
                    {
                        containers = new List<NetworkContainer>();
                        networkContainersWithItem.Add(item.techType, containers);
                    }

                    containers.Add(container);
                }
            }
        }

        public void RemoveContainer(NetworkContainer container)
        {
            networkContainers.Remove(container);

            if (container.ContainsItems())
            {
                List<InventoryItem> items = container.GetItems();
                foreach (InventoryItem item in items)
                {
                    List<NetworkContainer> containers = GetContainersContaining(item.techType, false);
                    if (containers == null) { return; }

                    containers.Remove(container);
                }
            }
        }

        public void OnContainerAddItem(NetworkContainer container, InventoryItem item)
        {
            TechType type = item.techType;
            List<NetworkContainer> containers = GetContainersContaining(type, false);
            if (containers == null)
            {
                containers = new List<NetworkContainer>();
                networkContainersWithItem.Add(type, containers);
            }
            if (!containers.Contains(container)) { containers.Add(container); }
        }

        public void OnContainerRemoveItem(NetworkContainer container, InventoryItem item)
        {
            TechType type = item.techType;
            if (container.Contains(item)) { return; } // Still has at least one item in storage

            List<NetworkContainer> containers = GetContainersContaining(type, false);
            if (containers == null) { return; }

            containers.Remove(container);
        }
    }
}
