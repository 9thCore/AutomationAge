using Nautilus.Utility;
using System;
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
        public ItemsContainer itemContainer { get; private set; }

        public override void OnAttach(GameObject module)
        {
            container.requesterAttached = true;

            if(!gameObject.TryGetComponent(out StorageContainer storageContainer))
            {
                throw new InvalidOperationException("missing StorageContainer component");
            }

            CoroutineHost.StartCoroutine(QueueAttach(storageContainer));
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

        public IEnumerator QueueAttach(StorageContainer storageContainer)
        {
            yield return new WaitUntil(() => storageContainer.container != null);

            itemContainer = storageContainer.container;
            itemContainer.isAllowedToAdd += (Pickupable pickupable, bool verbose) =>
            {
                return !itemContainer.Contains(pickupable.GetTechType());
            };
        }

        /*
        public override void SaveData(string id)
        {
            Dictionary<string, RequesterSaveData> requesterSaveData = SaveHandler.data.requesterSaveData;
            requesterSaveData[id] = new RequesterSaveData(this);
        }

        public override void RemoveSaveData(string id)
        {
            Dictionary<string, RequesterSaveData> requesterSaveData = SaveHandler.data.requesterSaveData;
            requesterSaveData.Remove(id);
        }

        public override void LoadSaveData(string id)
        {
            Dictionary<string, RequesterSaveData> requesterSaveData = SaveHandler.data.requesterSaveData;
            if(requesterSaveData.TryGetValue(id, out RequesterSaveData data))
            {
                data.LoadRequesterData(this);
            }
        }
        */

        public override void RemoveAttachable()
        {
            container.requesterAttached = false;
        }

        public void QueueRequest()
        {
            CoroutineHost.StartCoroutine(DelayedRequest());
        }

        public IEnumerator DelayedRequest()
        {
            yield return new WaitForSeconds(RequestDelay);
            if (!keepRequesting) { yield break; }
            if (!itemContainer.IsEmpty()) { Request(); }

            // Request stuff again
            QueueRequest();
        }

        public void Request()
        {
            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();

            // Request one of each at the same time
            foreach(KeyValuePair<TechType, ItemsContainer.ItemGroup> pair in itemContainer._items)
            {
                List<InventoryItem> items = pair.Value.items;
                InventoryItem item = items[0];

                if (item == null) { continue; }
                Pickupable pickupable = item.item;

                // Don't bother searching if we can't put it in anyway
                if (!container.HasRoomFor(pickupable)) { continue; }

                foreach (NetworkContainer networkContainer in data.networkContainers)
                {
                    if (!networkContainer.ContainsItems() || !networkContainer.Contains(item)) { continue; }
                    container.AddItem(pickupable);
                    networkContainer.RemoveItem(pickupable);
                    break;
                }
            }
        }
    }
}
