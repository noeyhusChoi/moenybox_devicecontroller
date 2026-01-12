using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Management.Devices
{
    internal sealed class SupervisorCommandExecutor
    {
        private readonly DeviceDescriptor _desc;

        public SupervisorCommandExecutor(DeviceDescriptor desc)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
        }

        public async Task<CommandResult> ExecuteAsync(
            IDevice? device,
            ITransport? transport,
            SemaphoreSlim gate,
            DeviceCommand cmd,
            CancellationToken ct,
            Action requestReconnect)
        {
            if (device is null)
                return CreateNotConnectedCommandResult();

            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var result = await device.ExecuteAsync(cmd, ct).ConfigureAwait(false);
                if (result.Success && cmd.Name.Equals("RESTART", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (transport is not null)
                            await transport.CloseAsync(ct).ConfigureAwait(false);
                    }
                    catch { }

                    requestReconnect();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                requestReconnect();
                return CreateTimeoutCommandResult();
            }
            catch (Exception)
            {
                requestReconnect();
                return CreateErrorCommandResult();
            }
            finally
            {
                gate.Release();
            }
        }

        private CommandResult CreateNotConnectedCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType)
                ? _desc.Model
                : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED");
            return new CommandResult(false, string.Empty, Code: code);
        }

        private CommandResult CreateTimeoutCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType)
                ? _desc.Model
                : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT");
            return new CommandResult(false, string.Empty, Code: code, Retryable: true);
        }

        private CommandResult CreateErrorCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType)
                ? _desc.Model
                : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR");
            return new CommandResult(false, string.Empty, Code: code);
        }
    }
}
