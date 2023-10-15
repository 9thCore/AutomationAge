using AutomationAge.Buildables.Items;
using Nautilus.Handlers;
using UnityEngine;

namespace AutomationAge.Systems.Blueprint
{
    internal class BlueprintEncoder : MonoBehaviour, IConstructable, IObstacle, IHandTarget
    {
        public const string BlueprintEncoderLabel = "BlueprintEncoderLabel";
        public const string BlueprintEncoderUse = "UseBlueprintEncoder";
        public const string BlueprintEncoderUseTooltip = "Tooltip_UseBlueprintEncoder";
        public const string PrinterBlueprintSlot = "Printer_BlueprintSlot";
        public const string PrinterAnySlot = "Printer_AnySlot";

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

        public static void CreateEquipmentSlots(uGUI_PDA pda)
        {
            if (blueprintEquipmentGO != null) { return; }

            // what do

            /*
            Transform parent = pda.transform.Find("Content/InventoryTab/Equipment");
            uGUI_InventoryTab tab = pda.tabInventory as uGUI_InventoryTab;
            uGUI_EquipmentSlot slot = tab.equipment.allSlots["Head"];

            blueprintEquipmentGO = new GameObject(PrinterBlueprintSlot);
            anyEquipmentGO = new GameObject(PrinterAnySlot);

            blueprintEquipmentGO.transform.SetParent(parent);
            anyEquipmentGO.transform.SetParent(parent);

            uGUI_EquipmentSlot blueprintSlot = blueprintEquipmentGO.AddComponent<uGUI_EquipmentSlot>();
            uGUI_EquipmentSlot anySlot = anyEquipmentGO.AddComponent<uGUI_EquipmentSlot>();

            blueprintSlot.background = slot.background;
            anySlot.background = slot.background;

            blueprintSlot.slot = PrinterBlueprintSlot;
            anySlot.slot = PrinterAnySlot;
            */

            /*
            Transform parent = pda.Find("Content/InventoryTab/Equipment");

            blueprintEquipmentGO.transform.SetParent(parent);
            anyEquipmentGO.transform.SetParent(parent);

            uGUI_EquipmentSlot blueprintSlot = blueprintEquipmentGO.AddComponent<uGUI_EquipmentSlot>();
            uGUI_EquipmentSlot anySlot = anyEquipmentGO.AddComponent<uGUI_EquipmentSlot>();

            blueprintSlot.slot = PrinterBlueprintSlot;
            anySlot.slot = PrinterAnySlot;
            */
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

            InventoryItem item = equipment.RemoveItem(PrinterAnySlot, true, false);
            InventoryItem blueprint = equipment.GetItemInSlot(PrinterBlueprintSlot);
            GameObject blueprintObj = blueprint.item.gameObject;
            if (!blueprintObj.TryGetComponent(out BlueprintIdentifier identifier))
            {
                Plugin.Logger.LogWarning("Blueprint does not have BlueprintIdentifier component??");
                equipment.RemoveItem(PrinterBlueprintSlot, true, false);
                Destroy(blueprintObj);
                return;
            }
            
            identifier.SetTech(item.techType);
            Destroy(item.item.gameObject);
        }

        public bool CheckItemExistence()
        {
            return equipment.GetItemInSlot(PrinterBlueprintSlot) != null && equipment.GetItemInSlot(PrinterAnySlot) != null;
        }

        public bool GetCompatibleSlot(EquipmentType type, out string slot)
        {
            if (type == GetBlueprintEquipment())
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
