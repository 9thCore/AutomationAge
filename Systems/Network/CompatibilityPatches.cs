using AutomationAge.Systems.Network.Item;
using HarmonyLib;
using UnityEngine;
using System;

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
            if (__instance.TryGetComponent(out NetworkItemRequester _)
                || __instance.TryGetComponent(out Planter _)) { return; }

            __instance.gameObject.EnsureComponent<NetworkContainer>().StorageContainer(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Equipment), new Type[] { typeof(GameObject), typeof(Transform) })]
        [HarmonyPatch(MethodType.Constructor)]
        public static void EquipmentPatch(Equipment __instance)
        {
            GameObject go = __instance.owner;
            if (!go.TryGetComponent(out NetworkContainer _))
            {
                if (go.TryGetComponent(out BaseNuclearReactor reactor))
                {
                    go.AddComponent<NetworkContainer>().NuclearReactor(reactor);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemsContainer), new Type[] { typeof(int), typeof(int), typeof(Transform), typeof(string), typeof(FMODAsset) })]
        [HarmonyPatch(MethodType.Constructor)]
        public static void ItemsContainerPatch(ItemsContainer __instance)
        {
            GameObject go = __instance.tr.gameObject.transform.parent?.gameObject;
            if (!go.TryGetComponent(out NetworkContainer _))
            {
                if (go.TryGetComponent(out BaseBioReactor reactor))
                {
                    go.AddComponent<NetworkContainer>().BioReactor(reactor);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SubRoot), nameof(SubRoot.Awake))]
        public static void SubRootPatch(SubRoot __instance)
        {
            __instance.gameObject.EnsureComponent<BaseData>();
        }
    }
}
