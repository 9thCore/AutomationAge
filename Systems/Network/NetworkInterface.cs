using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkInterface : AttachableModule
    {

        public override void OnAttach(GameObject module)
        {
            container.interfaceAttached = true;
        }

        public override void StartBehaviour()
        {
            container.StartBroadcasting();
        }

        public override void StopBehaviour()
        {
            container.StopBroadcasting();
        }

        public override void RemoveAttachable()
        {
            container.interfaceAttached = false;
        }
    }
}
