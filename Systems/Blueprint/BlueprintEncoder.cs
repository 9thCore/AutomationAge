using AutomationAge.Buildables.Items;
using Nautilus.Handlers;
using System.Collections;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems.Blueprint
{
    internal class BlueprintEncoder : MonoBehaviour, IConstructable, IObstacle, IHandTarget
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

        public static EquipmentType blueprintEquipmentType = EquipmentType.None;
        public static EquipmentType anyEquipmentType = EquipmentType.None;

        public static GameObject blueprintEquipmentGO = null;
        public static GameObject anyEquipmentGO = null;

        public static void CreateEquipment()
        {
            blueprintEquipmentType = EnumHandler.AddEntry<EquipmentType>(PrinterBlueprintSlot);
            Equipment.slotMapping[PrinterBlueprintSlot] = blueprintEquipmentType;
            anyEquipmentType = EnumHandler.AddEntry<EquipmentType>(PrinterAnySlot);
            Equipment.slotMapping[PrinterAnySlot] = anyEquipmentType;
        }

        public static void CreateEquipmentSlots(GameObject slotClone)
        {
            if (blueprintEquipmentGO != null) { return; }

            blueprintEquipmentGO = Instantiate(slotClone, slotClone.transform.parent);
            blueprintEquipmentGO.name = PrinterBlueprintSlot;
            blueprintEquipmentGO.transform.localPosition = BlueprintSlotPosition;

            anyEquipmentGO = Instantiate(slotClone, slotClone.transform.parent);
            anyEquipmentGO.name = PrinterAnySlot;
            anyEquipmentGO.transform.localPosition = AnySlotPosition;

            uGUI_EquipmentSlot blueprintSlot = blueprintEquipmentGO.GetComponent<uGUI_EquipmentSlot>();
            uGUI_EquipmentSlot anySlot = anyEquipmentGO.GetComponent<uGUI_EquipmentSlot>();

            blueprintSlot.slot = PrinterBlueprintSlot;
            anySlot.slot = PrinterAnySlot;
        }

        public static EquipmentType GetBlueprintEquipment()
        {
            if (blueprintEquipmentType == EquipmentType.None)
            {
                CreateEquipment();
            }
            return blueprintEquipmentType;
        }

        public void Start()
        {
            root = gameObject.FindChild(BlueprintImprinter.ItemRootName).GetComponent<ChildObjectIdentifier>();

            equipment = new Equipment(gameObject, root.transform);
            equipment.SetLabel(BlueprintEncoderLabel);
            equipment.isAllowedToAdd = new IsAllowedToAdd(IsAllowedToAdd);
            equipment.isAllowedToRemove = new IsAllowedToRemove(IsAllowedToRemove);
            equipment.compatibleSlotDelegate = new Equipment.DelegateGetCompatibleSlot(GetCompatibleSlot);
            equipment.onEquip += OnEquip;

            equipment.AddSlot(PrinterBlueprintSlot);
            equipment.AddSlot(PrinterAnySlot);
        }

        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return true;
        }

        public bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            return true;
        }

        public void OnEquip(string slot, InventoryItem _)
        {
            if (!CheckItemExistence()) { return; }

            CoroutineHost.StartCoroutine(TransferDataToBlueprint());
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

            SetData(identifier, item);
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
            if (type == blueprintEquipmentType)
            {
                slot = PrinterBlueprintSlot;
                return true;
            }

            slot = PrinterAnySlot;
            return true;
        }

        public bool CanDeconstruct(out string reason)
        {
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

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetText(HandReticle.TextType.Hand, BlueprintEncoderUse, false, GameInput.Button.LeftHand);
            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, BlueprintEncoderUseTooltip, true, GameInput.Button.None);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        public void OnHandClick(GUIHand hand)
        {
            Inventory.main.SetUsedStorage(equipment, false);
            Player.main.GetPDA().Open(PDATab.Inventory, transform, null);
        }
    }
}
