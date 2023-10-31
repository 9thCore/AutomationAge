using AutomationAge.Buildables.Items;
using AutomationAge.Items;
using AutomationAge.Systems.Attach;
using AutomationAge.Systems.Blueprint;
using AutomationAge.Systems.Network;
using Nautilus.Crafting;
using Nautilus.Handlers;
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
        public const string AutoCrafterLabel = "AutoCrafterLabel";
        public const string AutoCrafterBlueprintUse = "UseAutoCrafterBlueprint";
        public const string AutoCrafterBlueprintUseTooltip = "Tooltip_UseAutoCrafterBlueprint";
        public static float DurationMultiplier = 2f;
        public static float DefaultCraftDuration = 2.7f;

        private NetworkContainer container;
        private bool initialised = false;
        private StorageContainer inputContainer;
        private StorageContainer outputContainer;
        private CrafterSaveData crafterSaveData;

        public GenericHandTarget equipmentHandTarget;
        public Equipment equipment;
        public ChildObjectIdentifier blueprintRoot;

        public Dictionary<TechType, int> Ingredients = new Dictionary<TechType, int>();
        public Dictionary<TechType, int> IngredientsModifiable = new Dictionary<TechType, int>();

        public const string CrafterBlueprintSlot = "Crafter_BlueprintSlot";
        public static GameObject blueprintEquipmentGO = null;

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
            outputContainer = moduleAttachedTo.GetComponentInChildren<StorageContainer>();

            inputContainer.container.onAddItem += OnInput;
            outputContainer.container.isAllowedToAdd += (_, _) => false;

            initialised = true;

            blueprintRoot = gameObject.FindChild("BlueprintRoot").GetComponent<ChildObjectIdentifier>();
            equipment = new Equipment(gameObject, blueprintRoot.transform);
            equipment.SetLabel(AutoCrafterLabel);
            equipment.compatibleSlotDelegate = new Equipment.DelegateGetCompatibleSlot(GetCompatibleSlot);
            equipment.isAllowedToAdd = new IsAllowedToAdd(IsAllowedToAdd);
            equipment.onEquip += OnEquip;
            equipment.onUnequip += OnUnequip;

            equipment.AddSlot(CrafterBlueprintSlot);

            equipmentHandTarget = gameObject.FindChild("BlueprintInput").GetComponent<GenericHandTarget>();
            equipmentHandTarget.onHandClick.AddListener(OpenEquipmentPDA);
            equipmentHandTarget.onHandHover.AddListener(Hover);

            if (crafterSaveData.craftType != TechType.None)
            {
                CoroutineHost.StartCoroutine(CreateBlueprint(crafterSaveData.craftType));
            }
        }
        
        public static void CreateEquipmentSlots(GameObject slotClone)
        {
            if (blueprintEquipmentGO != null) { return; }

            blueprintEquipmentGO = Utility.CreateEquipmentSlot(slotClone, CrafterBlueprintSlot);
            Utility.MapEquipmentType(CrafterBlueprintSlot, BlueprintEncoder.blueprintEquipmentType);
        }

        public void OpenEquipmentPDA(HandTargetEventData data)
        {
            Inventory.main.SetUsedStorage(equipment, false);
            Player.main.GetPDA().Open(PDATab.Inventory, blueprintRoot.transform);
        }

        public IEnumerator CreateBlueprint(TechType type)
        {
            TaskResult<GameObject> task = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(ItemBlueprint.Info.TechType, task, false);

            GameObject obj = task.Get();
            obj.SetActive(false);

            BlueprintIdentifier identifier = obj.GetComponent<BlueprintIdentifier>();
            identifier.SetTech(type);

            Pickupable pickupable = obj.GetComponent<Pickupable>();
            InventoryItem item = new InventoryItem(pickupable);

            equipment.AddItem(CrafterBlueprintSlot, item, true);
        }

        public void Hover(HandTargetEventData data)
        {
            HandReticle.main.SetText(HandReticle.TextType.Hand, AutoCrafterBlueprintUse, true, GameInput.Button.LeftHand);
            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, AutoCrafterBlueprintUseTooltip, true, GameInput.Button.None);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            if (!pickupable.TryGetComponent(out BlueprintIdentifier identifier))
            {
                return false;
            }

            if (identifier.GetTech() == TechType.None)
            {
                return false;
            }

            return true;
        }

        public void OnEquip(string _, InventoryItem item)
        {
            if (!item.item.gameObject.TryGetComponent(out BlueprintIdentifier identifier))
            {
                return;
            }

            SetRecipe(identifier.GetTech());
            item.item.transform.parent = null;
        }
        
        public void OnUnequip(string _, InventoryItem __)
        {
            RemoveRecipe();
        }

        public bool GetCompatibleSlot(EquipmentType itemType, out string slot)
        {
            if (itemType == BlueprintEncoder.blueprintEquipmentType)
            {
                slot = CrafterBlueprintSlot;
                return true;
            }

            slot = "";
            return false;
        }

        public void OnInput(InventoryItem item)
        {
            TryStartCraft();
        }

        public void TryStartCraft()
        {
            if (CanStartCraft())
            {
                CoroutineHost.StartCoroutine(WaitThenStartCraft());
            }
        }

        public IEnumerator WaitThenStartCraft()
        {
            // Wait a frame for the tooltip to go away
            yield return null;

            StartCraft();
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
            Ingredients.Clear();
            crafterSaveData.byproducts.Clear();

            RecipeData data = CraftDataHandler.GetRecipeData(type);
            if (data == null)
            {
                Plugin.Logger.LogWarning("Could not set recipe, as type " + type + " does not have an associated recipe!");
                return false;
            }

            foreach (CraftData.Ingredient ingredient in data.Ingredients)
            {
                Ingredients.Add(ingredient.techType, ingredient.amount);
            }

            crafterSaveData.craftType = type;
            crafterSaveData.byproducts.AddRange(Enumerable.Repeat(type, data.craftAmount - 1)); // We're already including the first item
            crafterSaveData.byproducts.AddRange(data.LinkedItems);

            if (!CraftData.GetCraftTime(type, out crafterSaveData.craftDuration))
            {
                crafterSaveData.craftDuration = DefaultCraftDuration;
            }

            TryStartCraft();

            return true;
        }

        public void RemoveRecipe()
        {
            crafterSaveData.craftType = TechType.None;
            Ingredients.Clear();
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

        public void StartCraft()
        {
            if (!CheckAndGetIngredients(out List<Pickupable> ingredients)) { return; }

            foreach(Pickupable ingredient in ingredients)
            {
                inputContainer.container.RemoveItem(ingredient, true);
                Destroy(ingredient.gameObject);
            }

            crafterSaveData.crafting = true;
            crafterSaveData.craftElapsedTime = 0f;
        }

        public void Update()
        {
            if (!crafterSaveData.crafting) { return; }

            crafterSaveData.craftElapsedTime += Time.deltaTime / DurationMultiplier;
            if (HasRoomInOutput(crafterSaveData.craftType) && crafterSaveData.craftElapsedTime >= crafterSaveData.craftDuration)
            {
                crafterSaveData.craftElapsedTime = 0f;
                crafterSaveData.crafting = false;

                CoroutineHost.StartCoroutine(CreateOutput(crafterSaveData.craftType));
                foreach(TechType byproduct in crafterSaveData.byproducts)
                {
                    CoroutineHost.StartCoroutine(CreateOutput(byproduct));
                }
            }
        }

        public IEnumerator CreateOutput(TechType type)
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(type, result);

            GameObject go = result.Get();
            go.SetActive(false);

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
            return HasPower() && HasRecipe() && !IsCrafting();
        }

        public bool HasPower()
        {
            return true;
        }

        public bool HasRecipe()
        {
            return crafterSaveData.craftType != TechType.None;
        }

        public bool IsCrafting()
        {
            return crafterSaveData.crafting;
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

        public override bool CanDeconstruct(out string reason)
        {
            if (equipment.GetItemInSlot(CrafterBlueprintSlot) != null)
            {
                reason = StorageContainer.deconstructNonEmptyMessage;
                return false;
            }

            reason = "";
            return true;
        }
    }
}
