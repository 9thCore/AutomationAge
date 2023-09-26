using System.Collections;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Miner
{
    internal class BaseMiner : MonoBehaviour, IConstructable, IObstacle
    {
        public static readonly Vector3 SearchDistance = new Vector3(1f, 1f, 1f);
        public const string HasRockDeconstructMessage = "CannotDeconstructHasRock";
        public const float ExtrusionInterval = 1f;
        public const float ExtrusionPowerConsumption = 2f;
        public const int RockExtrusionTarget = 1;
        public const float RockSpawnHeight = 2f;
        public static readonly Vector3 RockSpawnStartPosition = new Vector3(0f, RockSpawnHeight - 0.5f, 0f);
        public static readonly Vector3 RockSpawnPosition = new Vector3(0f, RockSpawnHeight, 0f);
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
                        _saveData.rockID = string.Empty;
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

        public Driller drillAttachment = null;

        public PowerRelay powerRelay;

        public GameObject spawnedRock = null;
        public Pickupable spawnedPickupable = null;
        public float rockSpawnTimer = 0f;
        public float creationTime = Time.time;

        public delegate void OnRockSpawned();
        public event OnRockSpawned OnRockSpawn;

        public void Start()
        {
            InvokeRepeating("UpdateExtrusion", Random.value, ExtrusionInterval);
        }

        public void OnDestroy()
        {
            spawnedPickupable?.pickedUpEvent.RemoveHandler(this, PickedUp);
            if (constructable.constructedAmount <= 0f)
            {
                Unsave();
            }
        }

        public void UpdateExtrusion()
        {
            if (!constructable.constructed) { return; }

            if (SaveData.rockTechType != TechType.None)
            {
                if (spawnedRock == null)
                {
                    SaveData.rockTechType = TechType.None;
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

                SpawnRandomLoot();
                return;
            }

            powerRelay = TechLight.GetNearestValidRelay(gameObject);
        }

        public void SpawnRandomLoot()
        {
            TechType type = BiomeUtils.GetRandomBiomeLoot(Biome);

            if (type != TechType.None)
            {
                SaveData.rockTechType = type;
                CoroutineHost.StartCoroutine(SpawnRock());
            }
        }

        public void CatchUp()
        {
            if (!SaveData.MustCatchUp()) { return; }

            float difference = Time.time - SaveData.lastActiveTime;
            int spawns = (int)(difference / (ExtrusionInterval * RockExtrusionTarget));

            if (spawns < 1) { return; }

            if (HasDrillAttachment())
            {
                CoroutineHost.StartCoroutine(CatchUpRockSpawns(spawns));
            }
            else
            {
                if (spawnedRock != null) { return; }
                SpawnRandomLoot();
            }
        }

        public void Update()
        {
            CatchUp();
            SaveData.UpdateActiveTime();

            if (spawnedRock != null)
            {
                rockSpawnTimer += Time.deltaTime;
                spawnedRock.transform.localPosition = Vector3.Lerp(RockSpawnStartPosition, RockSpawnPosition, rockSpawnTimer / RockSpawnTime);
            }
        }

        public void FindSpawnedRock()
        {
            if (SaveData.rockTechType == TechType.None) { return; }

            Collider[] colliders = Physics.OverlapBox(transform.position + transform.up * RockSpawnHeight, SearchDistance);

            for (int i = 0; i < colliders.Length; i++)
            {
                Transform tr = colliders[i].transform;
                while(tr != null)
                {
                    if (tr.gameObject.TryGetComponent(out PrefabIdentifier identifier) && identifier.Id == SaveData.rockID)
                    {
                        InitialiseRock(tr.gameObject);
                        return;
                    }
                    tr = tr.parent;
                }
            }

            Plugin.Logger.LogError($"Could not find spawned rock. Assuming no spawned rock");
        }

        public void InitialiseRock(GameObject obj)
        {
            spawnedRock = obj;
            spawnedRock.transform.SetParent(transform);
            spawnedRock.transform.localPosition = RockSpawnStartPosition;
            spawnedRock.transform.localRotation = Quaternion.identity;
            rockSpawnTimer = 0f;
            if (spawnedRock.TryGetComponent(out spawnedPickupable))
            {
                spawnedPickupable.pickedUpEvent.AddHandler(this, PickedUp);
            }

            SaveData.rockID = spawnedRock.GetComponent<PrefabIdentifier>().Id;

            OnRockSpawn?.Invoke();
        }

        public IEnumerator SpawnRock()
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(SaveData.rockTechType, result, false);
            InitialiseRock(result.Get());
        }

        public IEnumerator CatchUpRockSpawns(int spawns)
        {
            for(int i = 0; i < spawns; i++)
            {
                if (!drillAttachment.CanMine()) { yield break; }

                TaskResult<GameObject> result = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(SaveData.rockTechType, result, false);
                drillAttachment.CatchUp(result.Get());
            }
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
            SaveData.rockID = string.Empty;
        }

        public bool CanDeconstruct(out string reason)
        {
            if (HasDrillAttachment())
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

        public bool HasDrillAttachment()
        {
            return drillAttachment != null;
        }

        public void Save()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.minerSaveData[prefabIdentifier.Id] = _saveData;
        }

        public bool Load()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            if (SaveHandler.data.minerSaveData.TryGetValue(prefabIdentifier.Id, out _saveData))
            {
                _saveData.miner = this;
                FindSpawnedRock();
                return true;
            }
            return false;
        }

        public void Unsave()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.minerSaveData.Remove(prefabIdentifier.Id);
        }
    }
}
