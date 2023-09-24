using AutomationAge.Systems.Miner;
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
        public Dictionary<string, MinerSaveData> minerSaveData = new Dictionary<string, MinerSaveData>();
    }

    internal class CatchUpSaveData
    {
        public const int ActiveThreshold = 4;

        [JsonIgnore]
        public int lastActiveFrame = Time.frameCount;
        [JsonIgnore]
        public float lastActiveTime = Time.time;

        public bool MustCatchUp()
        {
            if (lastActiveFrame < 0)
            {
                return false;
            }

            return Time.frameCount - lastActiveFrame > ActiveThreshold;
        }

        public void UpdateActiveTime()
        {
            lastActiveFrame = Time.frameCount;
            lastActiveTime = Time.time;
        }
    }

    internal class AttachableSaveData
    {
        [JsonIgnore]
        internal AttachableModule module;

        public string attachedID;
        public Vector3 attachedPos;
        public bool fullyConstructed;
        public AttachableModule.SpecialModule specialModule;
    }

    internal class MinerSaveData : CatchUpSaveData
    {
        [JsonIgnore]
        internal BaseMiner miner;

        public string rockID;

        public int rockExtrusion = 0;
        public TechType rockTechType = TechType.None;
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
                    AttachableSaveData attachableData = saveData.Value;
                    attachableData.fullyConstructed = attachableData.module.Constructable.constructed;
                }
            };

            data.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
            };
        }
    }
}
