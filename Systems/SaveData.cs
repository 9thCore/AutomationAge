using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Json.Attributes;
using Newtonsoft.Json;
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
        [JsonIgnore]
        internal AttachableModule module;

        public string attachedID;
        public Vector3 attachedPos;
        public bool fullyConstructed;

        [JsonConstructor]
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
            };

            data.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };
        }
    }
}
