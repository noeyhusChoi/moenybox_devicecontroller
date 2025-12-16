namespace DeviceController.Devices.Simulated
{
    public record RateParameter(double RateHz);

    public record SimulatedStatus(bool Ready, double TemperatureC, string? Note = null);
}
