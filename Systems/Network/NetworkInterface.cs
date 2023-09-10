using UnityEngine;
using UnityEngine.Rendering;

namespace AutomationAge.Systems.Network
{
    internal class NetworkInterface : AttachableModule
    {
        public const float IdlePowerConsumption = 1f;
        public const float IdlePowerConsumptionInterval = 1f;

        private float lastPowerConsumptionTime = Time.time;
        private PowerConsumer consumer;

        public override void OnAttach(GameObject module)
        {
            Container.interfaceAttached = true;
            consumer = gameObject.EnsureComponent<PowerConsumer>();
        }

        public void Update()
        {
            // The interface does not stop working when out of power
            // Only modules using networked containers check and consume power

            if (Time.time - lastPowerConsumptionTime > IdlePowerConsumptionInterval)
            {
                lastPowerConsumptionTime = Time.time;
                consumer.ConsumePower(IdlePowerConsumption, out float _);
            }
        }

        public override void StartBehaviour()
        {
            Container.StartBroadcasting();
        }

        public override void StopBehaviour()
        {
            Container.StopBroadcasting();
        }

        public override void RemoveAttachable()
        {
            Container.interfaceAttached = false;
        }
    }
}
