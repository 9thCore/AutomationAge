using System;
using System.Collections.Generic;
using static LootDistributionData;

namespace AutomationAge.Systems.Miner
{
    // Unfortunately, hardcoding is required when it comes to Subnautica
    // I was hoping for free crossmod compat but alas
    internal static class BiomeUtils
    {
        public static readonly HashSet<TechType> resourceTechTypes = new HashSet<TechType>()
        {
            TechType.LimestoneChunk,
            TechType.SandstoneChunk,
            TechType.ShaleChunk,
            TechType.Nickel
        };

        public static readonly Dictionary<string, List<TechType>> cachedResources = new Dictionary<string, List<TechType>>();

        public static LootDistributionData lootDistributionData;

        // https://github.com/ccgould/FCStudios_SubnauticaMods/blob/master/FCS_ProductionSolutions/Mods/DeepDriller/Managers/BiomeManager.cs#L56
        public static void GetBiome(string biome, out string biomeType)
        {
            if (biome.ToLower().EndsWith("Mesa"))
            {
                biomeType = "mesas";
            }
            else if (biome.ToLower().StartsWith("kelp"))
            {
                biomeType = "kelp";
            }
            else if (biome.ToLower().StartsWith("bloodkelp"))
            {
                biomeType = "bloodkelp";
            }
            else if (biome.ToLower().StartsWith("lostriver"))
            {
                biomeType = "lostriver";
            }
            else if (biome.ToLower().StartsWith("ilz"))
            {
                biomeType = "inactivelavazone";
            }
            else if (biome.ToLower().StartsWith("alz"))
            {
                biomeType = "activelavazone";
            }
            else if (biome.ToLower().StartsWith("lava"))
            {
                biomeType = "activelavazone";
            }
            else
            {
                biomeType = biome.ToLower();
            }
        }

        public static List<TechType> ComputeBiomeTech(string biome)
        {
            List<TechType> techs = new List<TechType>() { };
            GetBiome(biome, out string biomeType);

            if (cachedResources.TryGetValue(biomeType, out List<TechType> value)) { return value; }

            foreach(BiomeType type in Enum.GetValues(typeof(BiomeType)))
            {
                if (type.AsString(true).StartsWith(biomeType) && lootDistributionData.GetBiomeLoot(type, out DstData dstData))
                {
                    dstData.prefabs.ForEach(data =>
                    {
                        TechType type = CraftData.entClassTechTable.GetOrDefault(data.classId, TechType.None);
                        if (resourceTechTypes.Contains(type) && !techs.Contains(type)) { techs.Add(type); }
                    });
                }
            }

            // Special case
            if (biomeType == "lostriver")
            {
                techs.Add(TechType.Nickel);
            }

            if (techs.Count == 0) { techs.Add(TechType.None); }

            cachedResources.Add(biomeType, techs);
            return techs;
        }

        public static TechType GetRandomBiomeLoot(string biome)
        {
            List<TechType> resources = ComputeBiomeTech(biome);
            return resources.GetRandom();
        }
    }
}
