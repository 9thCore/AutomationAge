﻿using AutomationAge.Buildables.Items;
using AutomationAge.Items;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UWE;

namespace AutomationAge.Systems.Blueprint
{
    internal class BaseBlueprintEncoder : MonoBehaviour, IConstructable, IObstacle, IHandTarget
    {
        public const string BlueprintEncoderLabel = "BlueprintEncoderLabel";
        public const string BlueprintEncoderUse = "UseBlueprintEncoder";
        public const string BlueprintEncoderUseTooltip = "Tooltip_UseBlueprintEncoder";
        public const string PrinterBlueprintSlot = "Printer_BlueprintSlot";
        public const string PrinterAnySlot = "Printer_AnySlot";
        public static readonly Vector3 AnySlotPosition = new Vector3(0f, 100f, 0f);
        public static readonly Vector3 BlueprintSlotPosition = new Vector3(0f, -100f, 0f);

        public Equipment equipment;
        public ChildObjectIdentifier root;
        public PrefabIdentifier identifier;
        public BlueprintEncoderSaveData saveData;
        public GameObject screen;
        public Image screenBackground;
        public uGUI_ItemIcon screenIcon;

        public static EquipmentType anyEquipmentType = EquipmentHandler.CreateEquipmentType(PrinterAnySlot);

        public static GameObject blueprintEquipmentGO = null;
        public static GameObject anyEquipmentGO = null;

        public static void InitEquipment()
        {
            EquipmentHandler.InitEquipment += () =>
            {
                EquipmentHandler.MapEquipmentType(PrinterBlueprintSlot, ItemBlueprint.BlueprintEquipmentType);

                blueprintEquipmentGO = EquipmentHandler.CreateEquipmentSlot(PrinterBlueprintSlot, BlueprintSlotPosition);
                anyEquipmentGO = EquipmentHandler.CreateEquipmentSlot(PrinterAnySlot, AnySlotPosition);
            };
        }

        public void Start()
        {
            root = gameObject.FindChild(BlueprintEncoder.ItemRootName).GetComponent<ChildObjectIdentifier>();
            identifier = gameObject.GetComponent<PrefabIdentifier>();
            screen = gameObject.FindChild(BlueprintEncoder.ScreenName);

            screenBackground = screen.FindChild("Background").GetComponent<Image>();
            screenIcon = screen.FindChild("Icon").GetComponent<uGUI_ItemIcon>();
            screenIcon.SetForegroundSize(0.375f, 0.375f, true);

            equipment = new Equipment(gameObject, root.transform);
            equipment.SetLabel(BlueprintEncoderLabel);
            equipment.isAllowedToAdd = new IsAllowedToAdd(IsAllowedToAdd);
            equipment.isAllowedToRemove = new IsAllowedToRemove(IsAllowedToRemove);
            equipment.compatibleSlotDelegate = new Equipment.DelegateGetCompatibleSlot(GetCompatibleSlot);
            equipment.onEquip += OnEquip;
            equipment.onUnequip += OnUnequip;

            equipment.AddSlot(PrinterBlueprintSlot);
            equipment.AddSlot(PrinterAnySlot);
            
            if (!Load())
            {
                saveData = new BlueprintEncoderSaveData();
                saveData.encoder = this;
                Save();
            }

            // Add items to slots after load
            foreach(Pickupable pickupable in root.GetAllComponentsInChildren<Pickupable>())
            {
                InventoryItem item = new InventoryItem(pickupable);

                if (pickupable.TryGetComponent(out BlueprintIdentifier _))
                {
                    equipment.AddItem(PrinterBlueprintSlot, item, true);
                } else
                {
                    equipment.AddItem(PrinterAnySlot, item, true);
                }
            }

            if (saveData.Operating)
            {
                if (!CheckItemExistence())
                {
                    saveData.Operating = false;
                    saveData.OperationElapsed = 0f;
                    return;
                }

                CoroutineHost.StartCoroutine(TransferDataToBlueprint());
            }
        }

        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return true;
        }

        public bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            return !saveData.Operating;
        }

        public void OnEquip(string slot, InventoryItem item)
        {
            if (slot == PrinterAnySlot)
            {
                screenIcon.SetForegroundSprite(SpriteManager.Get(item.techType));
                screenIcon.SetForegroundAlpha(1f);
            }

            if (CheckItemExistence())
            {
                CoroutineHost.StartCoroutine(TransferDataToBlueprint());
            }
        }

        public void OnUnequip(string slot, InventoryItem _)
        {
            if (slot == PrinterAnySlot)
            {
                screenIcon.SetForegroundAlpha(0f);
            }
        }

        public bool CheckItemExistence()
        {
            return equipment.GetItemInSlot(PrinterBlueprintSlot) != null && equipment.GetItemInSlot(PrinterAnySlot) != null;
        }

        public IEnumerator TransferDataToBlueprint()
        {
            InventoryItem item = equipment.GetItemInSlot(PrinterAnySlot);
            InventoryItem blueprint = equipment.GetItemInSlot(PrinterBlueprintSlot);
            GameObject blueprintObj = blueprint.item.gameObject;

            if (!blueprintObj.TryGetComponent(out BlueprintIdentifier identifier))
            {
                Plugin.Logger.LogWarning("Blueprint does not have BlueprintIdentifier component??");
                RemoveItem(PrinterBlueprintSlot);
                yield break;
            }

            saveData.OperationDuration = CalculateDuration();
            saveData.Operating = true;

            while (saveData.OperationElapsed < saveData.OperationDuration)
            {
                saveData.OperationElapsed += Time.deltaTime;
                yield return null;
            }

            SetData(identifier, item);
            saveData.OperationElapsed = 0f;
            saveData.Operating = false;
        }

        public static float CalculateDuration()
        {
            return 1f;
        }

        public void SetData(BlueprintIdentifier identifier, InventoryItem item)
        {
            if (identifier.GetTech() == item.techType)
            {
                // do nothing if it would result in no change, so as to not waste items
                return;
            }

            identifier.SetTech(item.techType);
            RemoveItem(PrinterAnySlot);
        }

        public void RemoveItem(string slot)
        {
            Destroy(equipment.RemoveItem(slot, true, false).item.gameObject);
        }

        public bool GetCompatibleSlot(EquipmentType type, out string slot)
        {
            if (type == ItemBlueprint.BlueprintEquipmentType)
            {
                slot = PrinterBlueprintSlot;
                return true;
            }

            slot = PrinterAnySlot;
            return true;
        }

        public bool IsEmpty()
        {
            return equipment.GetTechTypeInSlot(PrinterBlueprintSlot) == TechType.None && equipment.GetTechTypeInSlot(PrinterAnySlot) == TechType.None;
        }

        public bool CanDeconstruct(out string reason)
        {
            if (!IsEmpty())
            {
                reason = StorageContainer.deconstructNonEmptyMessage;
                return false;
            }

            reason = "";
            return true;
        }

        public bool IsDeconstructionObstacle()
        {
            return true;
        }

        public void OnConstructedChanged(bool constructed)
        {
            
        }

        public void OnDestroy()
        {
            if (gameObject.TryGetComponent(out Constructable c) && c.constructedAmount <= 0f)
            {
                Unsave();
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetText(HandReticle.TextType.Hand, BlueprintEncoderUse, true, GameInput.Button.LeftHand);
            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, BlueprintEncoderUseTooltip, true, GameInput.Button.None);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        public void OnHandClick(GUIHand hand)
        {
            Inventory.main.SetUsedStorage(equipment, false);
            Player.main.GetPDA().Open(PDATab.Inventory, transform, null);
        }

        public bool Load()
        {
            if (SaveHandler.data.blueprintEncoderSaveData.TryGetValue(identifier.Id, out saveData))
            {
                saveData.encoder = this;
                return true;
            }
            return false;
        }

        public void Save()
        {
            SaveHandler.data.blueprintEncoderSaveData[identifier.Id] = saveData;
        }

        public void Unsave()
        {
            SaveHandler.data.blueprintEncoderSaveData.Remove(identifier.Id);
        }
    }
}
