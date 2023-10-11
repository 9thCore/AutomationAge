using AutomationAge.Buildables.Items;
using AutomationAge.Systems.Miner;
using AutomationAge.Systems.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.AutoCrafting
{
    internal class AutoCrafter : AttachableModule
    {
        public static float DurationMultiplier = 2f;
        public static float CraftDuration = 3f;

        private NetworkContainer container;
        private bool initialised = false;
        private StorageContainer inputContainer;
        private StorageContainer outputContainer;
        private CrafterSaveData crafterSaveData;
        private TechType recipeTech; // To-do: change this to an item down the line

        public Dictionary<TechType, int> Ingredients = new Dictionary<TechType, int>();
        public Dictionary<TechType, int> IngredientsModifiable = new Dictionary<TechType, int>();

        public override void OnAttach(GameObject module)
        {
            container = module.GetComponentInChildren<NetworkContainer>();
            if (container == null)
            {
                Plugin.Logger.LogWarning("Cannot install crafter on something without a container!");
                Destroy(this);
                return;
            }

            container.crafterAttached = true;
        }
        public override void RemoveAttachable()
        {
            if (container == null) { return; }

            container.crafterAttached = false;
        }

        public override void Start()
        {
            base.Start();

            inputContainer = gameObject.FindChild(AutoFabricator.InputContainerName).GetComponent<StorageContainer>();
            outputContainer = ModuleAttachedTo.GetComponentInChildren<StorageContainer>();

            inputContainer.container.onAddItem += OnInput;
            outputContainer.container.isAllowedToAdd += (_, _) => false;

            SetRecipe(TechType.Battery);

            initialised = true;
        }

        public void OnInput(InventoryItem item)
        {
            if (CanStartCraft())
            {
                CoroutineHost.StartCoroutine(WaitThenStartCraft(recipeTech));
            }
        }

        public IEnumerator WaitThenStartCraft(TechType type)
        {
            // Wait a frame for the tooltip to go away
            yield return new WaitForEndOfFrame();

            StartCraft(type);
        }

        public void CopyModifiableIngredients()
        {
            IngredientsModifiable.Clear();
            foreach(KeyValuePair<TechType, int> pair in Ingredients)
            {
                IngredientsModifiable.Add(pair.Key, pair.Value);
            }
        }

        public bool SetRecipe(TechType type)
        {
            ITechData data = CraftData.Get(type);
            if (data == null)
            {
                Plugin.Logger.LogWarning("Cannot get tech data!");
                return false;
            }

            Ingredients.Clear();
            for(int i = 0; i < data.ingredientCount; i++)
            {
                IIngredient ing = data.GetIngredient(i);
                Ingredients.Add(ing.techType, ing.amount);
            }

            recipeTech = type;

            return true;
        }

        public bool CheckAndGetIngredients(out List<Pickupable> itemsToRemove)
        {
            CopyModifiableIngredients();

            itemsToRemove = new List<Pickupable>();

            foreach (InventoryItem item in inputContainer.container)
            {
                if (IngredientsModifiable.ContainsKey(item.techType) && IngredientsModifiable[item.techType] > 0)
                {
                    itemsToRemove.Add(item.item);
                    IngredientsModifiable[item.techType]--;
                }
            }

            foreach (KeyValuePair<TechType, int> pair in IngredientsModifiable)
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
        }

        public override void StopBehaviour()
        {
            if (!initialised) { return; }

            inputContainer.gameObject.SetActive(false);
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
