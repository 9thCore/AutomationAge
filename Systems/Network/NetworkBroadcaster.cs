using UnityEngine;

namespace AutomationAge.Systems.Network
{
    internal class NetworkBroadcaster : MonoBehaviour
    {
        public bool isAttached = false;
        private bool broadcasting = false;

        public void StartBroadcasting()
        {
            if (broadcasting) { return; }
            broadcasting = true;

            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();
            data.BroadcastGameObject(gameObject);
        }

        public void StopBroadcasting()
        {
            if (!broadcasting) { return; }
            broadcasting = false;

            GameObject baseRoot = transform.parent.gameObject;
            BaseData data = baseRoot.EnsureComponent<BaseData>();
            data.StopBroadcastingGameObject(gameObject);
        }
    }
}
