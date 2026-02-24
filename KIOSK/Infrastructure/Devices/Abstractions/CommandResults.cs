namespace KIOSK.Device.Abstractions;

public static class CommandResults
{
    public static CommandResult NotConnected(string deviceKey)
        => CommandFailure(deviceKey, "NOT_CONNECTED");

    public static CommandResult Unknown(string deviceKey)
        => CommandFailure(deviceKey, "UNKNOWN_COMMAND");

    public static CommandResult InvalidPayload(string deviceKey)
        => CommandFailure(deviceKey, "INVALID_PAYLOAD");

    public static CommandResult Timeout(string deviceKey)
        => CommandFailure(deviceKey, "TIMEOUT", retryable: true);

    public static CommandResult Error(string deviceKey)
        => CommandFailure(deviceKey, "ERROR");

    private static CommandResult CommandFailure(string deviceKey, string detail, bool retryable = false)
    {
        var normalizedDeviceKey = Normalize(deviceKey, "UNKNOWN_DEVICE");
        var normalizedDetail = Normalize(detail, "UNKNOWN_DETAIL");

        return new CommandResult(
            false,
            string.Empty,
            Code: new ErrorCode("DEV", normalizedDeviceKey, "COMMAND", normalizedDetail),
            Retryable: retryable);
    }

    private static string Normalize(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
