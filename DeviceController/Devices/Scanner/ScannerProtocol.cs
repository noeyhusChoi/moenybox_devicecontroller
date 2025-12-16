using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;
using DeviceController.Services;

namespace DeviceController.Devices.Scanner
{
    public class ScannerProtocol : IDeviceProtocol<ScannerCommandId>
    {
        private const byte SourceHost = 0x04;
        private static readonly Dictionary<byte, string> NakCauses = new()
        {
            { 0x01, "RESEND" },
            { 0x02, "BAD_CONTEXT" },
            { 0x06, "DENIED" }
        };

        private readonly IDecodeEventBus _decodeBus;

        public ScannerProtocol(IDecodeEventBus decodeBus)
        {
            _decodeBus = decodeBus;
        }

        public IReadOnlyList<DeviceCommandMetadata> DescribeCommands() => new[]
        {
            new DeviceCommandMetadata(ScannerCommandId.ScanEnable, "Scan Enable", "Enable scanning.", false),
            new DeviceCommandMetadata(ScannerCommandId.ScanDisable, "Scan Disable", "Disable scanning.", false),
            new DeviceCommandMetadata(ScannerCommandId.ResetDevice, "Reset", "Reset scanner.", false),
            new DeviceCommandMetadata(ScannerCommandId.RequestRevision, "Request Revision", "Read model/FW/HW.", true),
            new DeviceCommandMetadata(ScannerCommandId.AckNakOn, "ACK/NAK On", "Enable software handshaking.", false),
            new DeviceCommandMetadata(ScannerCommandId.AutoInductionOn, "Auto-Induction On", "Enable automatic trigger.", false),
            new DeviceCommandMetadata(ScannerCommandId.PacketModeOn, "Packet Mode On", "Enable packet-mode decode data.", false)
        };

        public bool IsStatusCommand(ScannerCommandId commandId) => commandId == ScannerCommandId.RequestRevision;

        public async Task<CommandResult> ExecuteAsync(DeviceCommand<ScannerCommandId> command, IDeviceClient client, CancellationToken cancellationToken)
        {
            var (opcode, status, data) = BuildRequest(command);
            var frame = BuildFrame(opcode, SourceHost, status, data);
            var exchange = await client.ExchangeAsync(frame, cancellationToken).ConfigureAwait(false);
            if (!exchange.Success)
            {
                return CommandResult.Failed(exchange.Message ?? "Transport failed.");
            }

            var parse = ParseFrame(exchange.Payload.Span);
            if (!parse.success)
            {
                return CommandResult.Failed(parse.message ?? "Invalid frame.");
            }

            if (parse.opcode == 0xD1)
            {
                var cause = parse.data.Length > 0 ? parse.data[0] : (byte)0;
                var msg = $"NAK {(NakCauses.TryGetValue(cause, out var m) ? m : $"0x{cause:X2}")}";
                return CommandResult.Failed(msg);
            }

            if (parse.opcode == 0xD0)
            {
                return CommandResult.Completed("ACK");
            }

            if (parse.opcode == 0xA4 && command.CommandId == ScannerCommandId.RequestRevision)
            {
                var revision = ParseRevision(parse.data);
                if (revision == null)
                {
                    return CommandResult.Failed("Revision parse failed.");
                }

                return CommandResult.Completed("Revision received.", revision);
            }

            if (parse.opcode == 0xF4)
            {
                var decode = ParseDecode(parse.data);
                if (decode != null)
                {
                    _decodeBus.Publish(decode);
                }
                return CommandResult.Completed("Decode received.", decode);
            }

            return CommandResult.Rejected($"Unexpected opcode 0x{parse.opcode:X2}");
        }

        public DeviceStateSnapshot ApplyStatus(DeviceStateSnapshot state, DeviceCommand<ScannerCommandId> command, CommandResult result)
        {
            if (command.CommandId == ScannerCommandId.RequestRevision && result.Status == CommandStatus.Completed && result.Data is ScannerRevision rev)
            {
                var detail = $"{rev.Model} FW={rev.Firmware} HW={rev.Hardware}";
                return state.With(connection: ConnectionState.Connected, health: HealthState.Ready, detail: detail);
            }

            if (result.Status == CommandStatus.Failed || result.Status == CommandStatus.Timeout)
            {
                return state.With(health: HealthState.Degraded, detail: result.Message ?? "Scanner command failed.");
            }

            return state;
        }

        private static (byte opcode, byte status, byte[] data) BuildRequest(DeviceCommand<ScannerCommandId> command)
        {
            return command.CommandId switch
            {
                ScannerCommandId.ScanEnable => (0xE9, 0x00, Array.Empty<byte>()),
                ScannerCommandId.ScanDisable => (0xEA, 0x00, Array.Empty<byte>()),
                ScannerCommandId.ResetDevice => (0xA0, 0x00, Array.Empty<byte>()),
                ScannerCommandId.RequestRevision => (0xA3, 0x00, Array.Empty<byte>()),
                ScannerCommandId.AckNakOn => (0xC6, 0x08, new byte[] { 0x00, 0x9F, 0x01 }),
                ScannerCommandId.AutoInductionOn => (0xC6, 0x08, new byte[] { 0x00, 0x8A, 0x03 }),
                ScannerCommandId.PacketModeOn => (0xC6, 0x08, new byte[] { 0x00, 0xEE, 0x01 }),
                _ => throw new InvalidOperationException($"Unsupported command {command.CommandId}")
            };
        }

        private static byte[] BuildFrame(byte opcode, byte source, byte status, byte[] data)
        {
            // Length includes the length field(s) themselves and everything up to data (checksum excluded).
            var baseLength = 1 /*opcode*/ + 1 /*source*/ + 1 /*status*/ + data.Length;
            bool extended = baseLength + 1 > 0xFF; // +1 if single-length byte is used

            int totalLengthFieldValue;
            int headerLen;
            if (extended)
            {
                // total length excludes checksum but includes LenH/LenL
                totalLengthFieldValue = baseLength + 2; // LenH+LenL counted
                headerLen = 3;
            }
            else
            {
                totalLengthFieldValue = baseLength + 1; // Length byte counted
                headerLen = 1;
            }

            var totalWithoutChecksum = headerLen + baseLength;
            var buffer = new byte[totalWithoutChecksum + 2]; // + checksum
            var offset = 0;
            if (extended)
            {
                buffer[offset++] = 0xFF;
                buffer[offset++] = (byte)((totalLengthFieldValue >> 8) & 0xFF);
                buffer[offset++] = (byte)(totalLengthFieldValue & 0xFF);
            }
            else
            {
                buffer[offset++] = (byte)totalLengthFieldValue;
            }

            buffer[offset++] = opcode;
            buffer[offset++] = source;
            buffer[offset++] = status;

            if (data.Length > 0)
            {
                Array.Copy(data, 0, buffer, offset, data.Length);
                offset += data.Length;
            }

            var (high, low) = ComputeChecksum(buffer.AsSpan(0, offset));
            buffer[offset++] = high;
            buffer[offset] = low;
            return buffer;
        }

        private static (bool success, byte opcode, byte status, byte[] data, string? message) ParseFrame(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 5) return (false, 0, 0, Array.Empty<byte>(), "Frame too short.");

            int offset;
            int bodyLength;
            if (frame[0] == 0xFF)
            {
                if (frame.Length < 6) return (false, 0, 0, Array.Empty<byte>(), "Extended frame too short.");
                var lenValue = (frame[1] << 8) | frame[2]; // includes LenH/LenL, excludes checksum
                bodyLength = lenValue - 2; // minus LenH/LenL
                offset = 3;
            }
            else
            {
                var lenValue = frame[0]; // includes length byte itself
                bodyLength = lenValue - 1;
                offset = 1;
            }

            if (bodyLength < 3) return (false, 0, 0, Array.Empty<byte>(), "Payload too short.");

            var checksumIndex = offset + bodyLength;
            if (checksumIndex + 2 > frame.Length)
            {
                return (false, 0, 0, Array.Empty<byte>(), "Length mismatch.");
            }

            if (!VerifyChecksum(frame.Slice(0, checksumIndex + 2)))
            {
                return (false, 0, 0, Array.Empty<byte>(), "Checksum invalid.");
            }

            var payload = frame.Slice(offset, bodyLength);
            var opcode = payload[0];
            var status = payload[2];
            var data = payload.Length > 3 ? payload.Slice(3).ToArray() : Array.Empty<byte>();
            return (true, opcode, status, data, null);
        }

        private static (byte high, byte low) ComputeChecksum(ReadOnlySpan<byte> data)
        {
            int sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }
            var checksum = (ushort)((0x10000 - (sum & 0xFFFF)) & 0xFFFF);
            return ((byte)((checksum >> 8) & 0xFF), (byte)(checksum & 0xFF));
        }

        private static bool VerifyChecksum(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 3) return false;
            var data = frame[..^2];
            var provided = frame[^2..];
            var (h, l) = ComputeChecksum(data);
            return h == provided[0] && l == provided[1];
        }

        private static ScannerRevision? ParseRevision(ReadOnlySpan<byte> data)
        {
            var text = Encoding.ASCII.GetString(data);
            var parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 3)
            {
                return new ScannerRevision(parts[0], parts[1], parts[2]);
            }

            return new ScannerRevision(text, text, text);
        }

        private static ScannerDecodeData? ParseDecode(ReadOnlySpan<byte> data)
        {
            if (data.Length < 2) return null;
            var type = data[0];
            var payload = Encoding.ASCII.GetString(data.Slice(1));
            return new ScannerDecodeData(type, payload);
        }
    }
}
