using AutomationAge.Systems.Network.Item;
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
            // Blacklist certain objects from using the NetworkContainer
            if (__instance.TryGetComponent(out NetworkItemRequester _)) { return; }
            __instance.gameObject.EnsureComponent<NetworkContainer>().StorageContainer(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SubRoot), nameof(SubRoot.Awake))]
        public static void SubRootPatch(SubRoot __instance)
        {
            __instance.gameObject.EnsureComponent<BaseData>();
        }
    }
}
