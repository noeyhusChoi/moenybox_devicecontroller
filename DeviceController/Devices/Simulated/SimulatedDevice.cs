using DeviceController.Core.Base;
using DeviceController.Core.States;

namespace DeviceController.Devices.Simulated
{
    public class SimulatedDevice : DeviceBase<SimulatedCommandId>
    {
        public SimulatedDevice(string deviceId, SimulatedDeviceClient client, SimulatedProtocol protocol)
            : base(deviceId, client, protocol, DeviceStateSnapshot.Disconnected("Idle"))
        {
        }

        protected override bool IsAllowedInState(SimulatedCommandId commandId, DeviceStateSnapshot state)
        {
            // Example rule: do not allow Start when device is degraded.
            //if (commandId == SimulatedCommandId.Start && state.HealthState == HealthState.Degraded)
            //{
            //    return false;
            //}

            //return true;

            if (commandId == SimulatedCommandId.QueryStatus)
                return true; // 상태 확인은 항상 허용

            return state.HealthState == HealthState.Ready;
        }
    }
}
