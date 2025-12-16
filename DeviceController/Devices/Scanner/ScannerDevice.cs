using DeviceController.Core.Base;
using DeviceController.Core.States;

namespace DeviceController.Devices.Scanner
{
    public class ScannerDevice : DeviceBase<ScannerCommandId>
    {
        public ScannerDevice(string deviceId, ScannerClient client, ScannerProtocol protocol)
            : base(deviceId, client, protocol, DeviceStateSnapshot.Disconnected("Scanner idle"))
        {
        }

        protected override bool IsAllowedInState(ScannerCommandId commandId, DeviceStateSnapshot state)
        {
            // Allow status (RequestRevision) even when health is unknown/degraded to recover state.
            if (commandId == ScannerCommandId.RequestRevision)
            {
                return true;
            }

            // Other commands require health not Fault and Ready/Degraded allowed based on connection handled in base.
            if (state.HealthState == HealthState.Fault)
            {
                return false;
            }

            return true;
        }
    }
}
