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
        public string attachedID;
        public Vector3 attachedPos;

        public AttachableSaveData() { }

        public AttachableSaveData(AttachableModule module)
        {
            attachedID = module.attachedID;
            attachedPos = module.attachedPos;
        }

        public void LoadAttachableData(AttachableModule module)
        {
            module.attachedID = attachedID;
            module.attachedPos = attachedPos;
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
            };

            data.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };
        }
    }
}
