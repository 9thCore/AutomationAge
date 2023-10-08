using AutomationAge.Buildables.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.AutoCrafting
{
    internal class AutoCrafter : MonoBehaviour
    {
        public static float DurationMultiplier = 2f;
        public static float CraftDuration = 3f;

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
            if (CanStartCraft())
            {
                StartCraft(TechType.Battery);
            }
        }

        public bool CheckAndGetIngredients(out List<Pickupable> itemsToRemove)
        {
            itemsToRemove = new List<Pickupable>();
            Dictionary<TechType, int> ingClone = new Dictionary<TechType, int>();
            foreach (KeyValuePair<TechType, int> pair in Ingredients)
            {
                ingClone.Add(pair.Key, pair.Value);
            }

            foreach (InventoryItem item in InputContainer.container)
            {
                if (ingClone.ContainsKey(item.techType) && ingClone[item.techType] > 0)
                {
                    itemsToRemove.Add(item.item);
                    ingClone[item.techType]--;
                }
            }

            foreach (KeyValuePair<TechType, int> pair in ingClone)
            {
                if (pair.Value > 0)
                {
                    // Cannot craft, as we don't have enough ingredients
                    return false;
                }
            }

            return true;
        }

        public void StartCraft(TechType type)
        {
            List<Pickupable> ingredients;
            if (!CheckAndGetIngredients(out ingredients)) { return; }

            foreach(Pickupable ingredient in ingredients)
            {
                InputContainer.container.RemoveItem(ingredient, true);
                // Destroy(ingredient.gameObject);
            }
            
            SaveData.craftType = type;
            SaveData.craftElapsedTime = 0f;
        }

        public void Update()
        {
            if (SaveData.craftType == TechType.None)
            {
                return;
            }

            SaveData.craftElapsedTime += Time.deltaTime / DurationMultiplier;
            if (HasRoomInOutput(SaveData.craftType) && SaveData.craftElapsedTime >= CraftDuration)
            {
                CoroutineHost.StartCoroutine(CreateOutput(SaveData.craftType));
                SaveData.craftElapsedTime = 0f;
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
