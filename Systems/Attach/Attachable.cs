using UnityEngine;

namespace AutomationAge.Systems.Attach
{
    internal class Attachable : MonoBehaviour
    {
        public delegate bool CanConstructDelegate(GameObject input, out GameObject obj);
        public CanConstructDelegate CanConstruct;

        public bool AllowOnNonConstructables = false;

        public delegate bool SnappingRuleDelegate(GameObject input, ref Vector3 position, ref Quaternion rotation);
        public SnappingRuleDelegate SnappingRule;
    }
}
