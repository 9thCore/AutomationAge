using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Network.Item
{
    internal class NetworkItemRequester : AttachableNetworkModule
    {
        public const float RequestDelay = 1f;

        // Power it takes to search inside the network for items
        public const float SearchPowerConsumption = 0.5f;

        // Power it takes to request from a container
        public const float RequestPowerConsumption = 1f;

        private bool keepRequesting = false;
        private PowerConsumer consumer;

        private FilterContainer _filter;
        public FilterContainer Filter
        {
            get
            {
                if (_filter == null)
                {
                    _filter = new FilterContainer(gameObject);
                }
                return _filter;
            }
        }

        public override void OnAttach(GameObject module)
        {
            Container.requesterAttached = true;
            consumer = gameObject.EnsureComponent<PowerConsumer>();
        }

        public override void StartBehaviour()
        {
            keepRequesting = true;

            // Randomise request for each requester so they don't all run at the same time
            // Don't bother with accurate ordering tbh
            Invoke("QueueRequest", Random.value);
        }

        public override void StopBehaviour()
        {
            keepRequesting = false;
        }

        public override void RemoveAttachable()
        {
            Container.requesterAttached = false;
            Filter.OnDestroy();
        }

        public void QueueRequest()
        {
            CoroutineHost.StartCoroutine(DelayedRequest());
        }

        public IEnumerator DelayedRequest()
        {
            yield return new WaitForSeconds(RequestDelay);
            if (!keepRequesting) { yield break; }
            if (!Filter.container.IsEmpty()) { Request(); }

            // Request stuff again
            QueueRequest();
        }

        public void Request()
        {
            // Request one of each at the same time
            foreach (InventoryItem item in Filter.GetItems())
            {
                // We do not have enough power to search and request, so stop looking to avoid running out
                if (GameModeUtils.RequiresPower() && !consumer.HasPower(SearchPowerConsumption + RequestPowerConsumption)) { break; }

                Pickupable pickupable = item.item;

                // Don't bother searching if we can't put it in anyway
                if (!Container.HasRoomFor(pickupable) || !Container.AllowedToAdd(pickupable)) { continue; }

                // We're looking in the network, so use power even if we don't request anything :)
                consumer.ConsumePower(SearchPowerConsumption, out float _);

                List<NetworkContainer> containers = Data.GetContainersContaining(pickupable.GetTechType());
                if (containers == null) { continue; }

                foreach (NetworkContainer networkContainer in containers)
                {
                    if (networkContainer == Container               // Don't request from itself
                        || !networkContainer.AllowedToRemove(pickupable)) { continue; }

                    Pickupable removedPickupable = networkContainer.RemoveItem(pickupable.GetTechType());
                    Container.AddItem(removedPickupable);

                    // We requested from container, so consume more power
                    consumer.ConsumePower(RequestPowerConsumption, out float _);
                    break;
                }
            }
        }
    }
}
