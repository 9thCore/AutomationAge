using AutomationAge.Buildables.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.AutoCrafting
{
    internal class AutoCrafter : AttachableModule
    {
        public static float DurationMultiplier = 2f;
        public static float CraftDuration = 3f;

        private bool initialised = false;
        private StorageContainer inputContainer;
        private StorageContainer outputContainer;
        private CrafterSaveData crafterSaveData;

        public Dictionary<TechType, int> Ingredients = new Dictionary<TechType, int>()
        {
            { TechType.Titanium, 2 },
            { TechType.Copper, 1 }
        };

        public override void Start()
        {
            base.Start();

            inputContainer = gameObject.FindChild(AutoFabricator.InputContainerName).GetComponent<StorageContainer>();
            outputContainer = ModuleAttachedTo.GetComponentInChildren<StorageContainer>();

            inputContainer.container.onAddItem += OnInput;
            outputContainer.container.isAllowedToAdd += (_, _) => false;

            initialised = true;
        }

        public void OnInput(InventoryItem item)
        {
            if (CanStartCraft())
            {
                CoroutineHost.StartCoroutine(WaitThenStartCraft(TechType.Battery));
            }
        }

        public IEnumerator WaitThenStartCraft(TechType type)
        {
            // Wait a frame for the tooltip to go away
            yield return new WaitForEndOfFrame();

            StartCraft(type);
        }

        public bool CheckAndGetIngredients(out List<Pickupable> itemsToRemove)
        {
            itemsToRemove = new List<Pickupable>();
            Dictionary<TechType, int> ingClone = new Dictionary<TechType, int>();
            foreach (KeyValuePair<TechType, int> pair in Ingredients)
            {
                ingClone.Add(pair.Key, pair.Value);
            }

            foreach (InventoryItem item in inputContainer.container)
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
            if (!CheckAndGetIngredients(out List<Pickupable> ingredients)) { return; }

            foreach(Pickupable ingredient in ingredients)
            {
                inputContainer.container.RemoveItem(ingredient, true);
                Destroy(ingredient.gameObject);
            }
            
            crafterSaveData.craftType = type;
            crafterSaveData.craftElapsedTime = 0f;
        }

        public void Update()
        {
            if (crafterSaveData.craftType == TechType.None)
            {
                return;
            }

            crafterSaveData.craftElapsedTime += Time.deltaTime / DurationMultiplier;
            if (HasRoomInOutput(crafterSaveData.craftType) && crafterSaveData.craftElapsedTime >= CraftDuration)
            {
                CoroutineHost.StartCoroutine(CreateOutput(crafterSaveData.craftType));
                crafterSaveData.craftElapsedTime = 0f;
                crafterSaveData.craftType = TechType.None;
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

            outputContainer.container.UnsafeAdd(new InventoryItem(pickupable));
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
            return crafterSaveData.craftType != TechType.None;
        }

        public bool HasRoomInOutput(TechType type)
        {
            Vector2int size = CraftData.GetItemSize(type);
            return outputContainer.container.HasRoomFor(size.x, size.y);
        }

        public override void StartBehaviour()
        {
            if (!initialised) { return; }

            inputContainer.gameObject.SetActive(true);
            outputContainer.gameObject.SetActive(true);
        }

        public override void StopBehaviour()
        {
            if (!initialised) { return; }

            inputContainer.gameObject.SetActive(false);
            outputContainer.gameObject.SetActive(false);
        }

        public override void OnCreateSave(string id)
        {
            crafterSaveData = new CrafterSaveData();
            crafterSaveData.crafter = this;
        }

        public override void OnSave(string id)
        {
            SaveHandler.data.crafterSaveData[id] = crafterSaveData;
        }

        public override void OnLoad(string id)
        {
            if (SaveHandler.data.crafterSaveData.TryGetValue(id, out crafterSaveData))
            {
                crafterSaveData.crafter = this;
            }
        }

        public override void OnUnsave(string id)
        {
            SaveHandler.data.crafterSaveData.Remove(id);
        }
    }
}
