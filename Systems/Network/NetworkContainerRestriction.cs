using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkContainerRestriction : MonoBehaviour
    {
        public bool requesterAllowed = true;
        public bool interfaceAllowed = true;
        public bool crafterAllowed = false;

        public void Restrict(bool requesterAllowed = true, bool interfaceAllowed = true, bool crafterAllowed = false)
        {
            this.requesterAllowed = requesterAllowed;
            this.interfaceAllowed = interfaceAllowed;
            this.crafterAllowed = crafterAllowed;
        }
    }
}
