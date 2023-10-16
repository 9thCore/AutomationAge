using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AutomationAge.Systems.Blueprint
{
    // Because modding this game cannot be TOO easy...

    [HarmonyPatch]
    internal static class BlueprintPatches
    {
        public static uGUI_ItemIcon draggedItemOverlay = null;

        // Patch the DestroyItem method to grab the removed item before it's fully destroyed
        // and call TryRemoveBlueprintData with it
        // (also patch Trashcan.Update with the same transpiler because it's the same code)

        [HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.DestroyItem))]
        [HarmonyPatch(typeof(Trashcan), nameof(Trashcan.Update))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyItemTranspile(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo destroyInfo = typeof(UnityEngine.Object).GetMethod(nameof(UnityEngine.Object.Destroy), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(UnityEngine.Object) }, null);
            MethodInfo patchInfo = typeof(BlueprintPatches).GetMethod(nameof(TryRemoveBlueprintData), BindingFlags.Static | BindingFlags.Public);

            int insertIndex = -1;
            CodeInstruction getGameObject = null;
            CodeInstruction loadLocal = null;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];

                if (code.Calls(destroyInfo)) {
                    insertIndex = i - 2;
                    getGameObject = codes[i - 1];
                    loadLocal = codes[i - 2];
                    break;
                }
            }

            if (insertIndex > -1)
            {
                codes.Insert(insertIndex, new CodeInstruction(OpCodes.Call, patchInfo));
                codes.Insert(insertIndex, new CodeInstruction(getGameObject));
                codes.Insert(insertIndex, new CodeInstruction(loadLocal));
            }

            return codes.AsEnumerable();
        }

        public static void TryRemoveBlueprintData(GameObject obj)
        {
            if (obj.TryGetComponent(out BlueprintIdentifier _) && obj.TryGetComponent(out PrefabIdentifier identifier))
            {
                SaveHandler.data.blueprintSaveData.Remove(identifier.Id);
            }
        }

        [HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.OnAddItem))]
        [HarmonyPostfix]
        public static void OnAddItemPostfix(InventoryItem item, uGUI_ItemsContainer __instance)
        {
            GameObject obj = item.item.gameObject;
            if (!obj.TryGetComponent(out BlueprintIdentifier blueprint)) { return; }

            blueprint.AddOverlay(__instance);
        }

        [HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.OnRemoveItem))]
        [HarmonyPostfix]
        public static void OnRemoveItemPostfix(InventoryItem item)
        {
            GameObject obj = item.item.gameObject;
            if (!obj.TryGetComponent(out BlueprintIdentifier blueprint)) { return; }

            blueprint.RemoveOverlay();
        }

        [HarmonyPatch(typeof(uGUI_Equipment), nameof(uGUI_Equipment.OnEquip))]
        [HarmonyPostfix]
        public static void OnEquipPostfix(InventoryItem item, uGUI_Equipment __instance)
        {
            GameObject obj = item.item.gameObject;
            if (!obj.TryGetComponent(out BlueprintIdentifier blueprint) || !__instance.items.TryGetValue(item, out uGUI_EquipmentSlot slot)) { return; }

            blueprint.AddOverlay(null, slot.icon, true);
        }

        [HarmonyPatch(typeof(uGUI_Equipment), nameof(uGUI_Equipment.OnUnequip))]
        [HarmonyPrefix]
        public static void OnUnequipPrefix(InventoryItem item)
        {
            GameObject obj = item.item.gameObject;
            if (!obj.TryGetComponent(out BlueprintIdentifier blueprint)) { return; }

            blueprint.RemoveOverlay();
        }

        [HarmonyPatch(typeof(ItemDragManager), nameof(ItemDragManager.DragStart))]
        [HarmonyPostfix]
        public static void DragStartPostfix(InventoryItem item)
        {
            GameObject obj = item.item.gameObject;
            if (!obj.TryGetComponent(out BlueprintIdentifier blueprint)) { return; }
            if (blueprint.GetTech() == TechType.None) { return; }

            draggedItemOverlay = BlueprintIdentifier.CreateOverlay(null, ItemDragManager.instance.draggedIcon);
            draggedItemOverlay.transform.localPosition = Vector3.zero;
            BlueprintIdentifier.UpdateSprite(draggedItemOverlay, blueprint.GetTech());
        }

        [HarmonyPatch(typeof(ItemDragManager), nameof(ItemDragManager.DragStop))]
        [HarmonyPrefix]
        public static void DragStopPrefix()
        {
            if (draggedItemOverlay == null) { return; }

            UnityEngine.Object.Destroy(draggedItemOverlay.gameObject);
            draggedItemOverlay = null;
        }

        [HarmonyPatch(typeof(uGUI_Equipment), nameof(uGUI_Equipment.Awake))]
        [HarmonyPrefix]
        public static void AwakePrefix(uGUI_Equipment __instance)
        {
            foreach (uGUI_EquipmentSlot slot in __instance.GetComponentsInChildren<uGUI_EquipmentSlot>())
            {
                if (slot.slot == BaseNuclearReactor.slotIDs[0])
                {
                    BlueprintEncoder.CreateEquipmentSlots(slot.gameObject);
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Equipment), nameof(Equipment.IsCompatible))]
        [HarmonyPrefix]
        public static bool IsCompatiblePrefix(EquipmentType itemType, EquipmentType slotType, ref bool __result)
        {
            if (itemType != BlueprintEncoder.blueprintEquipmentType && slotType == BlueprintEncoder.anyEquipmentType)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
