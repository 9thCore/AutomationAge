using AutomationAge.Buildables.Transportation.Items;
using AutomationAge.Items.Equipment;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Utility;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AutomationAge
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            Logger = base.Logger;

            Assets.Prepare();

            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public void Start()
        {
            YeetKnifePrefab.Register();
            ItemInterface.Register();
        }
    }
}