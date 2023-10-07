using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkContainerRestriction : MonoBehaviour
    {
        public bool requesterAllowed = true;
        public bool interfaceAllowed = true;

        public void Restrict(bool requesterAllowed, bool interfaceAllowed)
        {
            this.requesterAllowed = requesterAllowed;
            this.interfaceAllowed = interfaceAllowed;
        }
    }
}
