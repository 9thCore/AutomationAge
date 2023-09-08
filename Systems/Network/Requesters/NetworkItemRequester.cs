using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Network.Requesters
{
    internal class NetworkItemRequester : AttachableModule
    {
        public const float RequestDelay = 1f;

        private bool keepRequesting = false;
        public FilterContainer filter = new FilterContainer();

        public override void OnAttach(GameObject module)
        {
            container.requesterAttached = true;
            filter.QueueAttach(gameObject);
        }

        public override void StartBehaviour()
        {
            keepRequesting = true;
            QueueRequest();
        }

        public override void StopBehaviour()
        {
            keepRequesting = false;
        }

        public override void RemoveAttachable()
        {
            container.requesterAttached = false;
            filter.OnDestroy();
        }

        public void QueueRequest()
        {
            CoroutineHost.StartCoroutine(DelayedRequest());
        }

        public IEnumerator DelayedRequest()
        {
            yield return new WaitForSeconds(RequestDelay);
            if (!keepRequesting) { yield break; }
            if (!filter.container.IsEmpty()) { Request(); }

            // Request stuff again
            QueueRequest();
        }

        public void Request()
        {
            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();

            // Request one of each at the same time
            foreach(InventoryItem item in filter.GetItems())
            {
                Pickupable pickupable = item.item;

                // Don't bother searching if we can't put it in anyway
                if (!container.HasRoomFor(pickupable) || !container.AllowedToAdd(pickupable)) { continue; }

                foreach (NetworkContainer networkContainer in data.networkContainers)
                {
                    if (networkContainer == container               // Don't request from itself
                        || !networkContainer.ContainsItems()        // Don't request if the container is not an item container
                        || !networkContainer.Contains(item)         // Don't request if the container does not contain this
                        || !networkContainer.AllowedToRemove(pickupable)) { continue; }

                    Pickupable removedPickupable = networkContainer.RemoveItem(pickupable.GetTechType());
                    container.AddItem(removedPickupable);
                    break;
                }
            }
        }
    }
}
