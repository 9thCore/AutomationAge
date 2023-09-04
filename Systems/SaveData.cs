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
        public Dictionary<string, InterfaceSaveData> interfaceSaveData = new Dictionary<string, InterfaceSaveData>();
    }

    internal class InterfaceSaveData
    {
        public string attachedID;
        public Vector3 attachedPos;

        public InterfaceSaveData() { }

        public InterfaceSaveData(NetworkInterface networkInterface)
        {
            attachedID = networkInterface.attachedID;
            attachedPos = networkInterface.attachedPos;
        }

        public void LoadInterfaceData(NetworkInterface networkInterface)
        {
            networkInterface.attachedID = attachedID;
            networkInterface.attachedPos = attachedPos;
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
