using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    [HarmonyPatch]
    internal static class CompatibilityPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StorageContainer), nameof(StorageContainer.Awake))]
        public static void StorageContainerPatch(StorageContainer __instance)
        {
            __instance.gameObject.EnsureComponent<NetworkContainer>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SubRoot), nameof(SubRoot.Awake))]
        public static void SubRootPatch(SubRoot __instance)
        {
            __instance.gameObject.EnsureComponent<BaseData>();
        }
    }
}
