using UnityEngine;

namespace AutomationAge.Systems.Network.Requesters
{
    internal class NetworkItemRequester : AttachableModule
    {

        public override void OnAttach(GameObject module)
        {
            container.requesterAttached = true;
        }

        public override void StartBehaviour()
        {
            
        }

        public override void StopBehaviour()
        {
            
        }

        public void OnDestroy()
        {
            container.requesterAttached = false;
        }
    }
}
