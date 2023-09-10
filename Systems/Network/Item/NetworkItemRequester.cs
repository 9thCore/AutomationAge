﻿using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Network.Item
{
    internal class NetworkItemRequester : AttachableModule
    {
        public const float RequestDelay = 1f;

        // Power it takes to search inside the network for items
        public const float SearchPowerConsumption = 0.5f;

        // Power it takes to request from a container
        public const float RequestPowerConsumption = 1f;

        private bool keepRequesting = false;
        private PowerConsumer consumer;
        public FilterContainer filter = new FilterContainer();

        public override void OnAttach(GameObject module)
        {
            Container.requesterAttached = true;
            filter.QueueAttach(gameObject);
            consumer = gameObject.EnsureComponent<PowerConsumer>();
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
            Container.requesterAttached = false;
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
            foreach (InventoryItem item in filter.GetItems())
            {
                // We do not have enough power to search and request, so stop looking to avoid running out
                if (!consumer.HasPower(SearchPowerConsumption + RequestPowerConsumption)) { break; }

                Pickupable pickupable = item.item;

                // Don't bother searching if we can't put it in anyway
                if (!Container.HasRoomFor(pickupable) || !Container.AllowedToAdd(pickupable)) { continue; }

                // We're looking in the network, so use power even if we don't request anything :)
                consumer.ConsumePower(SearchPowerConsumption, out float _);

                foreach (NetworkContainer networkContainer in data.networkContainers)
                {
                    if (networkContainer == Container               // Don't request from itself
                        || !networkContainer.ContainsItems()        // Don't request if the container is not an item container
                        || !networkContainer.Contains(item)         // Don't request if the container does not contain this
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
