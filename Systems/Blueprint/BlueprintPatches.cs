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
        // Patch the DestroyItem method to grab the removed item before it's fully destroyed
        // and call TryRemoveBlueprintData with it

        [HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.DestroyItem))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyItemTranspile(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo destroyInfo = typeof(UnityEngine.Object).GetMethod(nameof(UnityEngine.Object.Destroy), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(UnityEngine.Object) }, null);
            MethodInfo patchInfo = typeof(BlueprintPatches).GetMethod(nameof(TryRemoveBlueprintData), BindingFlags.Static | BindingFlags.Public);

            int insertIndex = -1;
            CodeInstruction getGameObject = null;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];

                if (code.Calls(destroyInfo)) {
                    code.operand = patchInfo;
                    insertIndex = i + 1;
                    getGameObject = codes[i - 1];
                    break;
                }
            }

            if (insertIndex > -1)
            {
                codes.Insert(insertIndex, new CodeInstruction(OpCodes.Call, destroyInfo));
                codes.Insert(insertIndex, new CodeInstruction(getGameObject));
            }
            
            return instructions.AsEnumerable();
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
        public static void OnRemoveItemPostfix(InventoryItem item, uGUI_ItemsContainer __instance)
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
        }
    }
}
