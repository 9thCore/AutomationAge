using AutomationAge.Buildables.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.AutoCrafting
{
    internal class AutoCrafter : MonoBehaviour
    {
        public static float DurationMultiplier = 2f;

        private bool Initialised = false;

        private StorageContainer InputContainer;

        private StorageContainer OutputContainer;

        private CrafterSaveData _saveData;
        public CrafterSaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    if (!Load())
                    {
                        _saveData = new CrafterSaveData();
                        _saveData.crafter = this;

                        Save();
                    }
                }
                return _saveData;
            }
        }

        public Dictionary<TechType, int> Ingredients = new Dictionary<TechType, int>()
        {
            { TechType.Titanium, 2 },
            { TechType.Copper, 1 }
        };

        public void Start()
        {
            InputContainer = gameObject.FindChild(AutoFabricator.InputContainerName).GetComponent<StorageContainer>();
            OutputContainer = gameObject.FindChild(AutoFabricator.OutputContainerName).GetComponent<StorageContainer>();

            InputContainer.container.onAddItem += OnInput;
            OutputContainer.container.isAllowedToAdd += (_, _) => false;

            Initialised = true;
        }

        public void OnDestroy()
        {
            InputContainer.container.onAddItem -= OnInput;
            if (GetComponent<Constructable>().constructedAmount <= 0f)
            {
                Unsave();
            }
        }

        public void OnInput(InventoryItem item)
        {
            if (CanStartCraft() && CheckRecipe())
            {
                StartCraft(TechType.Battery);
            }
        }

        public void StartCraft(TechType type)
        {
            foreach(KeyValuePair<TechType, int> pair in Ingredients)
            {
                int count = pair.Value;
                List<Pickupable> removedItems = new List<Pickupable>();

                foreach(InventoryItem item in InputContainer.container.GetItems(pair.Key))
                {
                    if (count-- == 0) { break; }
                    removedItems.Add(item.item);
                }

                foreach(Pickupable item in removedItems)
                {
                    InputContainer.container.RemoveItem(item, true);
                    Destroy(item.gameObject);
                }
            }

            SaveData.craftType = type;
            CraftData.GetCraftTime(type, out SaveData.craftDuration);
            SaveData.craftElapsedTime = 0f;

            Plugin.Logger.LogInfo(SaveData.craftDuration);
        }

        public void Update()
        {
            if (SaveData.craftType == TechType.None)
            {
                return;
            }

            SaveData.craftElapsedTime += Time.deltaTime / DurationMultiplier;
            if (HasRoomInOutput(SaveData.craftType) && SaveData.craftElapsedTime >= SaveData.craftDuration)
            {
                CreateOutput(SaveData.craftType);
                SaveData.craftElapsedTime = 0f;
                SaveData.craftDuration = 0f;
                SaveData.craftType = TechType.None;
            }
        }

        public IEnumerator CreateOutput(TechType type)
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(type, result);

            GameObject go = result.Get();
            Pickupable pickupable = go.GetComponent<Pickupable>();

            if (pickupable == null)
            {
                Destroy(go);
                throw new InvalidOperationException("Tech type " + type + " is not a pickupable!");
            }

            if (!OutputContainer.container.HasRoomFor(pickupable))
            {
                Destroy(go);
                yield break;
            }

            OutputContainer.container.UnsafeAdd(new InventoryItem(pickupable));
        }

        public bool CanStartCraft()
        {
            return HasPower() && !IsCrafting();
        }

        public bool HasPower()
        {
            return true;
        }

        public bool IsCrafting()
        {
            return SaveData.craftType != TechType.None;
        }

        public bool HasRoomInOutput(TechType type)
        {
            Vector2int size = CraftData.GetItemSize(type);
            return OutputContainer.container.HasRoomFor(size.x, size.y);
        }

        public bool CheckRecipe()
        {
            foreach(KeyValuePair<TechType, int> pair in Ingredients)
            {
                if (InputContainer.container.GetCount(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public void OnEnable()
        {
            if (!Initialised) { return; }

            InputContainer.gameObject.SetActive(true);
            OutputContainer.gameObject.SetActive(true);
        }

        public void OnDisable()
        {
            if (!Initialised) { return; }

            InputContainer.gameObject.SetActive(false);
            OutputContainer.gameObject.SetActive(false);
        }

        public void Save()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.crafterSaveData[prefabIdentifier.Id] = _saveData;
        }

        public bool Load()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            if (SaveHandler.data.crafterSaveData.TryGetValue(prefabIdentifier.Id, out _saveData))
            {
                _saveData.crafter = this;
                return true;
            }
            return false;
        }

        public void Unsave()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.crafterSaveData.Remove(prefabIdentifier.Id);
        }
    }
}
