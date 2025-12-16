namespace DeviceController.Devices.Diagnostics
{
    public record CalibrateParameter(double OffsetMv);

    public record DiagnosticsStatus(bool Ready, double Voltage, string Firmware);
}
