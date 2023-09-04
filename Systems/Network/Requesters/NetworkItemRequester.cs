using UnityEngine;

namespace AutomationAge.Systems.Network.Requesters
{
    internal class NetworkItemRequester : AttachableModule
    {
        private NetworkContainer container = null;

        public override void OnAttach(GameObject module)
        {
            container = module.EnsureComponent<NetworkContainer>();
            container.requesterAttached = true;
        }

        public void Start()
        {

        }

        public void OnDestroy()
        {
            container.requesterAttached = false;
        }
    }
}
