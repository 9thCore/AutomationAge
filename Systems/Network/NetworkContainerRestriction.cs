using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkContainerRestriction : MonoBehaviour
    {
        public bool requesterAllowed = true;
        public bool interfaceAllowed = true;
        public bool crafterAllowed = true;

        public void Restrict(bool requesterAllowed = true, bool interfaceAllowed = true, bool crafterAllowed = true)
        {
            this.requesterAllowed = requesterAllowed;
            this.interfaceAllowed = interfaceAllowed;
            this.crafterAllowed = crafterAllowed;
        }
    }
}
