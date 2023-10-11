using UnityEngine;

namespace AutomationAge.Systems.Network
{
    public class AttachableNetworkModule : AttachableModule
    {

        private NetworkContainer _container;
        internal NetworkContainer Container => _container ??= ModuleAttachedTo?.GetComponent<NetworkContainer>();

        public GameObject PrefabRoot = null;

        private BaseData _data;
        internal BaseData Data => _data ??= PrefabRoot.transform.parent.gameObject.EnsureComponent<BaseData>();

        public override void Start()
        {
            if (PrefabRoot != null)
            {
                PrefabRoot = gameObject;
            }

            base.Start();
        }
    }
}
