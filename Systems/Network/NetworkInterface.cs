using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkInterface : AttachableModule
    {
        private NetworkContainer container = null;

        public override void OnAttach(GameObject module)
        {
            container = module.EnsureComponent<NetworkContainer>();
            container.interfaceAttached = true;
        }

        public void Start()
        {
            if (container == null) { Attach(); }
            container.StartBroadcasting();
        }

        public void OnEnable()
        {
            if (container == null) { return; }
            container.StartBroadcasting();
        }

        public void OnDisable()
        {
            if (container == null) { return; }
            container.StopBroadcasting();
        }

        public void OnDestroy()
        {
            container.interfaceAttached = false;
        }
    }
}
