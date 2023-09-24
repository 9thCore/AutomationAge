namespace AutomationAge.Systems.Network
{
    public class AttachableNetworkModule : AttachableModule
    {

        private NetworkContainer _container;
        internal NetworkContainer Container => _container ??= ModuleAttachedTo?.GetComponent<NetworkContainer>();

        private BaseData _data;
        internal BaseData Data => _data ??= transform.parent.gameObject.EnsureComponent<BaseData>();
    }
}
