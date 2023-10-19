using AutomationAge.Buildables.Items;
using AutomationAge.Systems.Attach;
using AutomationAge.Systems.Blueprint;
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
        public const string AutoCrafterLabel = "AutoCrafterLabel";
        public const string AutoCrafterBlueprintUse = "UseAutoCrafterBlueprint";
        public const string AutoCrafterBlueprintUseTooltip = "Tooltip_UseAutoCrafterBlueprint";
        public static float DurationMultiplier = 2f;
        public static float CraftDuration = 3f;

        private NetworkContainer container;
        private bool initialised = false;
        private StorageContainer inputContainer;
        private StorageContainer outputContainer;
        private CrafterSaveData crafterSaveData;
        private TechType recipeTech;

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
            outputContainer = ModuleAttachedTo.GetComponentInChildren<StorageContainer>();

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
        }

        public static void CreateEquipmentSlots(GameObject slotClone)
        {
            if (blueprintEquipmentGO != null) { return; }

            blueprintEquipmentGO = Instantiate(slotClone, slotClone.transform.parent);
            blueprintEquipmentGO.name = CrafterBlueprintSlot;
            blueprintEquipmentGO.transform.localPosition = Vector3.zero;

            uGUI_EquipmentSlot blueprintSlot = blueprintEquipmentGO.GetComponent<uGUI_EquipmentSlot>();

            blueprintSlot.slot = CrafterBlueprintSlot;

            Equipment.slotMapping[CrafterBlueprintSlot] = BlueprintEncoder.blueprintEquipmentType;
        }

        public void OpenEquipmentPDA(HandTargetEventData data)
        {
            Inventory.main.SetUsedStorage(equipment, false);
            Player.main.GetPDA().Open(PDATab.Inventory, blueprintRoot.transform);
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
            if (CanStartCraft())
            {
                CoroutineHost.StartCoroutine(WaitThenStartCraft(recipeTech));
            }
        }

        public IEnumerator WaitThenStartCraft(TechType type)
        {
            // Wait a frame for the tooltip to go away
            yield return null;

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

        public void RemoveRecipe()
        {
            recipeTech = TechType.None;
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
