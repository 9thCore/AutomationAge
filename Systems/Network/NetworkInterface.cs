using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    // Generic class in case I ever decide to add more functionality than item automation :]
    internal class NetworkInterface : AttachableModule
    {
        private NetworkBroadcaster broadcaster = null;

        public override void OnAttach(GameObject module)
        {
            broadcaster = module.EnsureComponent<NetworkBroadcaster>();
            broadcaster.isAttached = true;
        }

        public void Start()
        {
            if (broadcaster == null) { Attach(); }
            broadcaster.StartBroadcasting();
        }

        public void OnEnable()
        {
            if (broadcaster == null) { return; }
            broadcaster.StartBroadcasting();
        }

        public void OnDisable()
        {
            if (broadcaster == null) { return; }
            broadcaster.StopBroadcasting();
        }

        public void OnDestroy()
        {
            broadcaster.isAttached = false;
        }

        public override void Load()
        {
            Dictionary<string, InterfaceSaveData> interfaceSaveData = SaveHandler.data.interfaceSaveData;
            string id = gameObject.GetComponent<PrefabIdentifier>().id;
            if (interfaceSaveData.TryGetValue(id, out InterfaceSaveData data))
            {
                data.LoadInterfaceData(this);
            }
        }

        public override void Save()
        {
            Dictionary<string, InterfaceSaveData> interfaceSaveData = SaveHandler.data.interfaceSaveData;
            string id = gameObject.GetComponent<PrefabIdentifier>().id;
            interfaceSaveData[id] = new InterfaceSaveData(this);
        }
    }
}
