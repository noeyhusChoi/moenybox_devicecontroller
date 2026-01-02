using System;

namespace KIOSK.Device.Abstractions
{
    public static class CommandResultFactory
    {
        public static CommandResult UnknownCommand(string deviceKey)
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
                throw new ArgumentException("deviceKey is required.", nameof(deviceKey));

            return new CommandResult(
                false,
                string.Empty,
                Code: new ErrorCode("DEV", deviceKey.ToUpperInvariant(), "COMMAND", "UNKNOWN"));
        }
    }
}
