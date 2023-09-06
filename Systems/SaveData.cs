using AutomationAge.Systems.Network;
using AutomationAge.Systems.Network.Requesters;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Json.Attributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutomationAge.Systems
{
    [FileName("save_data")]
    internal class SaveData : SaveDataCache
    {
        public Dictionary<string, AttachableSaveData> attachableSaveData = new Dictionary<string, AttachableSaveData>();
        public Dictionary<string, RequesterSaveData> requesterSaveData = new Dictionary<string, RequesterSaveData>();
    }

    internal class AttachableSaveData
    {
        internal AttachableModule module;

        public string attachedID;
        public Vector3 attachedPos;
        public bool fullyConstructed;

        public AttachableSaveData() { }

        public AttachableSaveData(AttachableModule module)
        {
            SaveAttachableData(module);
        }

        public void SaveAttachableData(AttachableModule module)
        {
            this.module = module;
            attachedID = module.attachedID;
            attachedPos = module.attachedPos;
            if (module.TryGetComponent(out Constructable constructable))
            {
                fullyConstructed = constructable.constructed;
            }
        }

        public void LoadAttachableData(AttachableModule module)
        {
            this.module = module;
            module.attachedID = attachedID;
            module.attachedPos = attachedPos;
            module.fullyConstructed = fullyConstructed;
        }
    }

    internal class RequesterSaveData
    {
        internal NetworkItemRequester requester;
        public List<TechType> items = new List<TechType>();

        public RequesterSaveData(NetworkItemRequester requester)
        {
            SaveRequesterData(requester);
        }

        public void SaveRequesterData(NetworkItemRequester requester)
        {
            this.requester = requester;
            items = requester.items.ToList<TechType>();
        }

        public void LoadRequesterData(NetworkItemRequester requester)
        {
            requester.items = items.ToHashSet<TechType>();
        }
    }

    internal static class SaveHandler
    {
        public static SaveData data;

        internal static void Register()
        {
            data = SaveDataHandler.RegisterSaveDataCache<SaveData>();

            data.OnFinishedLoading += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };

            data.OnStartedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;

                foreach (KeyValuePair<string, AttachableSaveData> saveData in data.attachableSaveData)
                {
                    saveData.Value.SaveAttachableData(saveData.Value.module);
                }

                foreach (KeyValuePair<string, RequesterSaveData> saveData in data.requesterSaveData)
                {
                    saveData.Value.SaveRequesterData(saveData.Value.requester);
                }

            };

            data.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };
        }
    }
}
