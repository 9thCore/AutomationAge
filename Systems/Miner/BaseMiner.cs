using Nautilus.Handlers;
using Story;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Miner
{
    internal class BaseMiner : MonoBehaviour, IConstructable, IObstacle
    {
        public const string HasRockDeconstructMessage = "CannotDeconstructHasRock";
        public const float ExtrusionInterval = 1f;
        public const float ExtrusionPowerConsumption = 2f;
        public const int RockExtrusionTarget = 30;
        public static readonly Vector3 RockSpawnStartPosition = new Vector3(0f, 1.5f, 0f);
        public static readonly Vector3 RockSpawnPosition = new Vector3(0f, 2f, 0f);
        public const float RockSpawnTime = 0.5f;

        private Constructable _constructable;
        public Constructable constructable => _constructable ??= gameObject.EnsureComponent<Constructable>();

        private MinerSaveData _saveData;
        public MinerSaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    if (!Load())
                    {
                        _saveData = new MinerSaveData();
                        _saveData.miner = this;
                        _saveData.rockExtrusion = 0;
                        _saveData.rockTechType = TechType.None;

                        Save();
                    }
                }
                return _saveData;
            }
        }

        private string _biome;
        private string Biome => _biome ??= LargeWorld.main.GetBiome(transform.position);

        public bool hasDrillAttachment = false;

        public PowerRelay powerRelay;

        private bool isRockSpawned = false;
        public GameObject spawnedRock = null;
        public Pickupable spawnedPickupable = null;
        public float rockSpawnTimer = 0f;

        public delegate void OnRockSpawned();
        public event OnRockSpawned OnRockSpawn;

        public void Start()
        {
            InvokeRepeating("UpdateExtrusion", 0f, ExtrusionInterval);
        }

        public void OnDestroy()
        {
            Unsave();
            spawnedPickupable?.pickedUpEvent.RemoveHandler(this, PickedUp);
        }

        public void UpdateExtrusion()
        {
            if (!constructable.constructed) { return; }
            if (SaveData.rockTechType != TechType.None)
            {
                if (!isRockSpawned)
                {
                    isRockSpawned = true;
                    CoroutineHost.StartCoroutine(SpawnRock());
                }
                else
                {
                    // Check if destroyed or picked up
                    if (spawnedRock == null)
                    {
                        SaveData.rockTechType = TechType.None;
                    }
                }
                return;
            }

            if (!GameModeUtils.RequiresPower() || powerRelay != null)
            {
                if (powerRelay != null)
                {
                    if (!(powerRelay.GetPower() > ExtrusionPowerConsumption)) { return; }
                    powerRelay.ConsumeEnergy(ExtrusionPowerConsumption, out _);
                }

                SaveData.rockExtrusion++;
                if (SaveData.rockExtrusion < RockExtrusionTarget) { return; }
                SaveData.rockExtrusion = 0;

                TechType type = BiomeUtils.GetRandomBiomeLoot(Biome);

                if (type != TechType.None)
                {
                    SaveData.rockTechType = type;
                    isRockSpawned = true;

                    CoroutineHost.StartCoroutine(SpawnRock());
                }

                return;
            }

            powerRelay = TechLight.GetNearestValidRelay(gameObject);
        }

        public void Update()
        {
            if (spawnedRock != null && rockSpawnTimer < RockSpawnTime)
            {
                rockSpawnTimer += Time.deltaTime;
                spawnedRock.transform.localPosition = Vector3.Lerp(RockSpawnStartPosition, RockSpawnPosition, rockSpawnTimer / RockSpawnTime);
            }
        }

        public IEnumerator SpawnRock()
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(SaveData.rockTechType, result, false);
            spawnedRock = result.Get();
            spawnedRock.transform.SetParent(transform);
            spawnedRock.transform.localPosition = RockSpawnStartPosition;
            spawnedRock.transform.localRotation = Quaternion.identity;
            rockSpawnTimer = 0f;
            if (spawnedRock.TryGetComponent(out spawnedPickupable))
            {
                spawnedPickupable.pickedUpEvent.AddHandler(this, PickedUp);
            }

            OnRockSpawn?.Invoke();
        }

        public void PickedUp(Pickupable _)
        {
            PickedUp();
        }

        public void PickedUp()
        {
            spawnedRock = null;
            spawnedPickupable?.pickedUpEvent.RemoveHandler(this, PickedUp);
            spawnedPickupable = null;
        }

        public bool CanDeconstruct(out string reason)
        {
            if (hasDrillAttachment)
            {
                reason = Language.main.Get(ConstructableOnSpecificModules.DeconstructAttachedMessage);
                return false;
            }

            if (SaveData.rockTechType != TechType.None)
            {
                reason = Language.main.Get(HasRockDeconstructMessage);
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool IsDeconstructionObstacle()
        {
            return true;
        }

        public void OnConstructedChanged(bool constructed)
        {

        }

        public void Save()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.minerSaveData[prefabIdentifier.Id] = SaveData;
        }

        public bool Load()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            return SaveHandler.data.minerSaveData.TryGetValue(prefabIdentifier.Id, out _saveData);
        }

        public void Unsave()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.minerSaveData.Remove(prefabIdentifier.Id);
        }
    }
}
