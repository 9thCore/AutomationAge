﻿using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class BaseData : MonoBehaviour
    {
        List<StorageContainer> storageContainers = new List<StorageContainer>();

        public void BroadcastGameObject(GameObject obj)
        {
            if (obj.TryGetComponent(out StorageContainer sContainer))
            {
                storageContainers.Add(sContainer);
                UpdateContainers();
                return;
            }
        }

        public void StopBroadcastingGameObject(GameObject obj)
        {
            if (obj.TryGetComponent(out StorageContainer sContainer))
            {
                storageContainers.Remove(sContainer);
                UpdateContainers();
                return;
            }
        }

        public void UpdateContainers()
        {
            Plugin.Logger.LogInfo("Containers: [");
            storageContainers.ForEach(Plugin.Logger.LogInfo);    
            Plugin.Logger.LogInfo("]");
        }
    }
}
