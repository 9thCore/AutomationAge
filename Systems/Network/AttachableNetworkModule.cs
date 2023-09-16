namespace AutomationAge.Systems.Network
{
    public class AttachableNetworkModule : AttachableModule
    {

        private NetworkContainer _container;
        internal NetworkContainer Container => _container ??= ModuleAttachedTo.GetComponent<NetworkContainer>();

        private BaseData _data;
        internal BaseData Data => _data ??= transform.parent.gameObject.EnsureComponent<BaseData>();

        private bool firstRun = true;

        public virtual void StartBehaviour() { }
        public virtual void StopBehaviour() { }

        public override void PostLoad()
        {
            firstRun = false;
            if (SaveData.fullyConstructed) { StartBehaviour(); }
        }

        public void OnEnable()
        {
            if (firstRun) { return; }
            StartBehaviour();
        }

        public void OnDisable()
        {
            if (firstRun)
            {
                firstRun = false;
                return;
            }
            StopBehaviour();
        }
    }
}
