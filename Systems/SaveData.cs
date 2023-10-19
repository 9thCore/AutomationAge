﻿using AutomationAge.Systems.Attach;
using AutomationAge.Systems.AutoCrafting;
using AutomationAge.Systems.Blueprint;
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
        public Dictionary<string, CrafterSaveData> crafterSaveData = new Dictionary<string, CrafterSaveData>();
        public Dictionary<string, BlueprintSaveData> blueprintSaveData = new Dictionary<string, BlueprintSaveData>();
        public Dictionary<string, BlueprintEncoderSaveData> blueprintEncoderSaveData = new Dictionary<string, BlueprintEncoderSaveData>();
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

    internal class CrafterSaveData
    {
        [JsonIgnore]
        internal AutoCrafter crafter;

        public TechType craftType = TechType.None;
        public bool crafting = false;
        public float craftElapsedTime = 0f;
    }

    internal class BlueprintSaveData
    {
        [JsonIgnore]
        internal BlueprintIdentifier identifier;

        public TechType CopiedType = TechType.None;
    }

    internal class BlueprintEncoderSaveData
    {
        [JsonIgnore]
        internal BlueprintEncoder encoder;

        public bool Operating = false;
        public float OperationDuration = 0f;
        public float OperationElapsed = 0f;
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
