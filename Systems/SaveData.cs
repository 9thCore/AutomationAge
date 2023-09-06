using AutomationAge.Systems.Network;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Json.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems
{
    [FileName("save_data")]
    internal class SaveData : SaveDataCache
    {
        public Dictionary<string, AttachableSaveData> attachableSaveData = new Dictionary<string, AttachableSaveData>();
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
            this.module = module;
            attachedID = module.attachedID;
            attachedPos = module.attachedPos;
            if(module.TryGetComponent(out Constructable constructable))
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
                foreach(KeyValuePair<string, AttachableSaveData> saveData in data.attachableSaveData)
                {
                    AttachableSaveData attachableSaveData = saveData.Value;
                    AttachableModule module = attachableSaveData.module;
                    if(module == null) { continue; }

                    if(!module.TryGetComponent(out Constructable constructable)) {  continue; }
                    attachableSaveData.fullyConstructed = constructable.constructed;
                }
            };

            data.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };
        }
    }
}
