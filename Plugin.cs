﻿using AutomationAge.Buildables.Items;
using AutomationAge.Buildables.Items.Network;
using AutomationAge.Items;
using AutomationAge.Systems;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using System.IO;
using System.Reflection;

namespace AutomationAge
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private void Awake()
        {
            Logger = base.Logger;

            Assets.Prepare();
            SaveHandler.Register();

            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME}, v{PluginInfo.PLUGIN_VERSION} is loaded!");
        }

        public void Start()
        {
            ItemInterface.Register();
            ItemRequester.Register();
            Miner.Register();
            RockDriller.Register();
            AutoFabricator.Register();
            ItemBlueprint.Register();
            BlueprintEncoder.Register();

            LanguageHandler.RegisterLocalizationFolder("Language");
        }
    }
}