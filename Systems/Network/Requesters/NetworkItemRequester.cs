﻿using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Network.Requesters
{
    internal class NetworkItemRequester : AttachableModule, IHandTarget
    {
        public const float RequestDelay = 1f;
        public const string HoverText = "OpenStorage";
        public const string FilterText = "ConfigureFilter";
        public const string StorageLabel = "StorageLabel";
        public const int Width = 2;
        public const int Height = 2;

        private bool keepRequesting = false;
        internal HashSet<TechType> items = new HashSet<TechType>();
        public ItemsContainer itemContainer { get; private set; }

        public override void OnAttach(GameObject module)
        {
            container.requesterAttached = true;

            itemContainer = new ItemsContainer(Width, Height, transform, StorageLabel, null);
            itemContainer.isAllowedToAdd += (Pickupable pickupable, bool verbose) => { return !items.Contains(pickupable.inventoryItem.techType); };

            itemContainer.onAddItem += item => items.Add(item.techType);
            itemContainer.onRemoveItem += item => items.Remove(item.techType);
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
            if (!itemContainer.IsFull()) { Request(); }

            // Request stuff again
            QueueRequest();
        }

        public void Request()
        {
            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();

            // Request one of each at the same time
            foreach (TechType type in items)
            {
                foreach (NetworkContainer networkContainer in data.networkContainers)
                {
                    if (!networkContainer.ContainsItems() || !networkContainer.Contains(type)) { continue; }
                    container.AddItem(networkContainer.RemoveItem(type));
                    break;
                }
            }
        }

        public bool IsEmpty()
        {
            return itemContainer.IsEmpty();
        }

        void IHandTarget.OnHandHover(GUIHand hand)
        {
            if (enabled && gameObject.TryGetComponent(out Constructable constructable))
            {
                HandReticle.main.SetText(HandReticle.TextType.Hand, HoverText, translate: true, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, FilterText, translate: true);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        void IHandTarget.OnHandClick(GUIHand hand)
        {
            if (enabled && gameObject.TryGetComponent(out Constructable constructable))
            {
                Open();
            }
        }

        public void Open()
        {
            PDA pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(itemContainer);
            pda.Open(PDATab.Inventory, transform, OnClosePDA);
        }

        public void OnClosePDA(PDA pda)
        {
        }
    }
}
