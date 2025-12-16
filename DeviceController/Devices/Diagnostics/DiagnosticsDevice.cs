using DeviceController.Core.Base;
using DeviceController.Core.States;

namespace DeviceController.Devices.Diagnostics
{
    public class DiagnosticsDevice : DeviceBase<DiagnosticsCommandId>
    {
        public DiagnosticsDevice(string deviceId, DiagnosticsDeviceClient client, DiagnosticsProtocol protocol)
            : base(deviceId, client, protocol, DeviceStateSnapshot.Disconnected("Idle"))
        {
        }

        protected override bool IsAllowedInState(DiagnosticsCommandId commandId, DeviceStateSnapshot state)
        {
            if (state.HealthState == HealthState.Fault)
            {
                return false;
            }

            if (commandId == DiagnosticsCommandId.Reset && state.ConnectionState != ConnectionState.Connected)
            {
                return false;
            }

            return true;
        }
    }
}
