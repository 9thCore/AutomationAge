using HarmonyLib;

namespace AutomationAge.Systems.Miner
{
    [HarmonyPatch]
    internal static class MinerPatch
    {
        // Surely there was an easily accessible place where I could have grabbed it from but I could not find it
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LootDistributionData), nameof(LootDistributionData.Initialize))]
        internal static void GrabLootDistributionData(LootDistributionData __instance)
        {
            BiomeUtils.lootDistributionData = __instance;
        }
    }
}
