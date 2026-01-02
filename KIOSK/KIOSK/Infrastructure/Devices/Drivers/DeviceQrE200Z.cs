using System.IO;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using System.Text;

namespace KIOSK.Device.Drivers
{

    public sealed class DeviceQrE200Z : DeviceBase
    {
        private readonly List<byte> _rxBuffer = new();
        private readonly object _lock = new();
        private CancellationTokenSource? _rxCts;
        private Task? _rxTask;
        private int _failThreshold;
        private string? _lastRevision;
        private ITransport? _transport;

        public event Action<string>? Log;
        public event EventHandler<DecodeMessage>? Decoded;

        public DeviceQrE200Z(DeviceDescriptor descriptor, ITransport transport)
            : base(descriptor, transport)
        {
            _transport = transport;
        }

        public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                var transport = RequireTransport();
                _transport = transport;

                if (transport.IsOpen)
                    await transport.CloseAsync(ct).ConfigureAwait(false);

                await transport.OpenAsync(ct).ConfigureAwait(false);
                _failThreshold = 0;

                if (transport.IsOpen)
                {
                    // 기존에 돌던 수신 루프 있으면 정리
                    _rxCts?.Cancel();
                    try { if (_rxTask != null) await _rxTask.ConfigureAwait(false); } catch { /* ignore */ }

                    // Transport 끊김 시 수신 루프 중단
                    transport.Disconnected += (_, __) => { try { _rxCts?.Cancel(); } catch { } };

                    _rxCts = new CancellationTokenSource();
                    _rxTask = Task.Run(() => ReceiveLoopAsync(_rxCts.Token));

                    // 기본 설정 예시:
                    // - Packet 모드 (0xEE=0x01)
                    // - Host Trigger 모드 (0x8A=0x08) 또는 Auto-Induction(0x09) 등
                    await SetDecodeDataPacketFormatAsync(0x01, true, ct).ConfigureAwait(false);     // Packet Mode
                    await SetAutoInductionTriggerModeAsync(true, ct).ConfigureAwait(false);         // Auto-Induction
                    await ScanDisableAsync(ct).ConfigureAwait(false);                               // Scan Disable     

                    // Revision 요청(응답은 수신 루프에서 처리)
                    //await RequestRevisionAsync(ct).ConfigureAwait(false);

                    return CreateSnapshot();
                }
                else
                {
                    return CreateSnapshot(new[]
                    {
                        CreateAlarm("00", "QR 스캐너 미연결", Severity.Error)
                    });
                }
            }
            catch (Exception ex)
            {
                _failThreshold++;
                Log?.Invoke($"[E200Z] Initialize error: {ex.Message}");
                return CreateSnapshot(new[]
                {
                    CreateAlarm("00", "QR 스캐너 초기화 실패", Severity.Error)
                });
            }
        }

        public override async Task<DeviceStatusSnapshot> GetStatusAsync(
            CancellationToken ct = default,
            string temp = "")
        {
            var alarms = new List<DeviceAlarm>();

            try
            {
                // 간단하게 통신 체크용으로 REQUEST_REVISION 한번 날려봄
                var ok = await PingAsync(ct).ConfigureAwait(false);
                if (!ok)
                {
                    _failThreshold++;
                }
                else
                {
                    _failThreshold = 0;
                }
            }
            catch
            {
                _failThreshold++;
            }

            if (_failThreshold > 5)
                alarms.Add(CreateAlarm("01", "QR 스캐너 통신오류", Severity.Warning));

            return CreateSnapshot(alarms);
        }

        public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            try
            {
                using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

                switch (command)
                {
                    // 스캔 제어 계열
                    case { Name: string name } when name.Equals("SCAN_ENABLE", StringComparison.OrdinalIgnoreCase):
                        await ScanEnableAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("SCAN_DISABLE", StringComparison.OrdinalIgnoreCase):
                        await ScanDisableAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("START_DECODE", StringComparison.OrdinalIgnoreCase):
                        await StartDecodeAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("STOP_DECODE", StringComparison.OrdinalIgnoreCase):
                        await StopDecodeAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("RESET", StringComparison.OrdinalIgnoreCase):
                        await ResetAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    // 트리거 모드 / 파라미터 설정
                    case { Name: string name } when name.Equals("SET_HOST_TRIGGER", StringComparison.OrdinalIgnoreCase):
                        await SetHostTriggerModeAsync(true, ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("SET_AUTO_TRIGGER", StringComparison.OrdinalIgnoreCase):
                        await SetAutoInductionTriggerModeAsync(true, ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("SET_PACKET_MODE", StringComparison.OrdinalIgnoreCase):
                        await SetDecodeDataPacketFormatAsync(0x01, true, ct).ConfigureAwait(false);
                        return new CommandResult(true);

                    case { Name: string name } when name.Equals("REQUEST_REVISION", StringComparison.OrdinalIgnoreCase):
                        await RequestRevisionAsync(ct).ConfigureAwait(false);
                        return new CommandResult(true, Data: _lastRevision);
                }

                return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
            }
            catch (OperationCanceledException)
            {
                return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[E200Z] ExecuteAsync error: {ex.Message}");
                return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
            }
        }

        #region SSI 명령 헬퍼 (비동기 송신)

        private async Task SendPacketAsync(SsiPacket packet, CancellationToken ct)
        {
            var bytes = packet.ToBytes();
            var transport = RequireTransport();

            await transport.WriteAsync(bytes, ct).ConfigureAwait(false);
            Log?.Invoke($"[E200Z] TX: {BitConverter.ToString(bytes)}");
        }

        private async Task ScanEnableAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.SCAN_ENABLE), ct).ConfigureAwait(false);

        private async Task ScanDisableAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.SCAN_DISABLE), ct).ConfigureAwait(false);

        private async Task StartDecodeAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.START_DECODE), ct).ConfigureAwait(false);

        private async Task StopDecodeAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.STOP_DECODE), ct).ConfigureAwait(false);

        private async Task ResetAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.RESET), ct).ConfigureAwait(false);

        private async Task RequestRevisionAsync(CancellationToken ct) =>
            await SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.REQUEST_REVISION), ct).ConfigureAwait(false);

        private async Task SetHostTriggerModeAsync(bool saveToFlash, CancellationToken ct)
        {
            // Param 0x8A, Value=0x08 (Host Trigger)
            var pkt = SsiPacket.CreateParamByte(0x8A, 0x08, saveToFlash);
            await SendPacketAsync(pkt, ct).ConfigureAwait(false);
        }

        private async Task SetAutoInductionTriggerModeAsync(bool saveToFlash, CancellationToken ct)
        {
            // Param 0x8A, Value=0x09 (Auto Induction)
            var pkt = SsiPacket.CreateParamByte(0x8A, 0x09, saveToFlash);
            await SendPacketAsync(pkt, ct).ConfigureAwait(false);
        }

        private async Task SetDecodeDataPacketFormatAsync(byte value, bool saveToFlash, CancellationToken ct)
        {
            // Param 0xEE, value: 0x00(Raw), 0x01(Packet)
            var pkt = SsiPacket.CreateParamByte(0xEE, value, saveToFlash);
            await SendPacketAsync(pkt, ct).ConfigureAwait(false);
        }

        private async Task SetSameCodeDelayAsync(byte value, bool saveToFlash, CancellationToken ct)
        {
            // Param 0xF3: 같은 코드 재스캔 딜레이
            var pkt = SsiPacket.CreateParamByte(0xF3, value, saveToFlash);
            await SendPacketAsync(pkt, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 간단한 통신 확인용 Ping (REQUEST_REVISION 한 번 보내고 예외 여부만 확인).
        /// </summary>
        private async Task<bool> PingAsync(CancellationToken ct)
        {
            try
            {
                await RequestRevisionAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 수신 루프 및 SSI 파싱

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var transport = RequireTransport();
            var buf = new byte[256];

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int read = await transport.ReadAsync(buf, ct).ConfigureAwait(false);
                    if (read <= 0)
                        continue;

                    Log?.Invoke($"[E200Z] RAW: {BitConverter.ToString(buf, 0, read)}");

                    lock (_lock)
                    {
                        for (int i = 0; i < read; i++)
                            _rxBuffer.Add(buf[i]);
                    }

                    ProcessBuffer();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    // 무시 후 계속
                }
                catch (IOException)
                {
                    try { _rxCts?.Cancel(); } catch { }
                    break;
                }
                catch (InvalidOperationException)
                {
                    try { _rxCts?.Cancel(); } catch { }
                    break;
                }
                catch (Exception ex)
                {
                    try { _rxCts?.Cancel(); } catch { }
                    break;
                }
            }
        }

        private void ProcessBuffer()
        {
            while (true)
            {
                byte first;
                lock (_lock)
                {
                    if (_rxBuffer.Count < 2)
                        return;
                    first = _rxBuffer[0];
                }

                if (first == 0xFF)
                {
                    // Extended packet
                    if (!TryParseExtendedPacket(out var opcode, out var data, out var source, out var status))
                        return;

                    HandlePacket(opcode, data, source, status, extended: true);
                }
                else
                {
                    if (!TryParseStandardPacket(out var packet))
                        return;

                    HandlePacket((SsiOpcode)packet.Opcode, packet.Data, packet.Source, packet.Status, extended: false);
                }
            }
        }

        /// <summary>
        /// STD SSI 패킷 파싱 (Length 1바이트).
        /// </summary>
        private bool TryParseStandardPacket(out SsiPacket packet)
        {
            packet = null;
            lock (_lock)
            {
                if (_rxBuffer.Count < 6)
                    return false;

                byte length = _rxBuffer[0];
                int totalLen = length + 2; // 메시지(length) + 체크섬(2)

                if (_rxBuffer.Count < totalLen)
                    return false;

                ushort expected = SsiPacket.ComputeChecksum(_rxBuffer.ToArray(), 0, length);
                ushort recv = (ushort)((_rxBuffer[totalLen - 2] << 8) | _rxBuffer[totalLen - 1]);

                if (expected != recv)
                {
                    Log?.Invoke($"[E200Z] Checksum error (STD). Exp={expected:X4}, Recv={recv:X4}");
                    _rxBuffer.RemoveAt(0); // 한 바이트 버리고 다시 시도
                    return false;
                }

                byte opcode = _rxBuffer[1];
                byte source = _rxBuffer[2];
                byte status = _rxBuffer[3];
                int dataLen = length - 4;
                var data = new byte[dataLen];
                if (dataLen > 0)
                    _rxBuffer.CopyTo(4, data, 0, dataLen);

                packet = new SsiPacket
                {
                    Length = length,
                    Opcode = opcode,
                    Source = source,
                    Status = status,
                    Data = data,
                    Checksum = recv
                };

                _rxBuffer.RemoveRange(0, totalLen);
                Log?.Invoke($"[E200Z] RX(STD): Op=0x{opcode:X2}, Src=0x{source:X2}, Len={length}, DataLen={dataLen}");
                return true;
            }
        }

        /// <summary>
        /// EXT SSI 패킷 파싱 (Length1=0xFF, Length2=2바이트).
        /// Length2 = 체크섬 제외 전체 메시지 길이(FF ~ 마지막 Data까지).
        /// </summary>
        private bool TryParseExtendedPacket(out SsiOpcode opcode, out byte[] data, out byte source, out byte status)
        {
            opcode = 0;
            data = Array.Empty<byte>();
            source = 0;
            status = 0;

            lock (_lock)
            {
                if (_rxBuffer.Count < 7)
                    return false;

                if (_rxBuffer[0] != 0xFF)
                    return false;

                ushort length2 = (ushort)((_rxBuffer[2] << 8) | _rxBuffer[3]);

                int totalLen = length2 + 2; // 메시지(length2) + 체크섬(2)
                if (_rxBuffer.Count < totalLen)
                    return false;

                ushort expected = SsiPacket.ComputeChecksum(_rxBuffer.ToArray(), 0, length2);
                ushort recv = (ushort)((_rxBuffer[length2] << 8) | _rxBuffer[length2 + 1]);

                if (expected != recv)
                {
                    Log?.Invoke($"[E200Z] Checksum error (EXT). Exp={expected:X4}, Recv={recv:X4}");
                    _rxBuffer.RemoveAt(0);
                    return false;
                }

                byte opcode2 = _rxBuffer[4];
                source = _rxBuffer[5];
                status = _rxBuffer[6];

                int dataLen = length2 - 7;
                var dataBuf = new byte[dataLen];
                if (dataLen > 0)
                    _rxBuffer.CopyTo(7, dataBuf, 0, dataLen);

                data = dataBuf;
                opcode = (SsiOpcode)opcode2;

                _rxBuffer.RemoveRange(0, totalLen);
                Log?.Invoke($"[E200Z] RX(EXT): Op=0x{opcode2:X2}, Src=0x{source:X2}, Len2={length2}, DataLen={dataLen}");
                return true;
            }
        }

        private void HandlePacket(SsiOpcode opcode, byte[] data, byte source, byte status, bool extended)
        {
            switch (opcode)
            {
                case SsiOpcode.DECODE_DATA:
                case SsiOpcode.DECODE_DATA_TWO:
                    HandleDecode(opcode, data, extended);
                    _ = SendAckAsync(source);   // fire-and-forget
                    break;

                case SsiOpcode.REPLY_REVISION:
                    HandleRevisionReply(data);
                    break;

                case SsiOpcode.CMD_ACK:
                    Log?.Invoke("[E200Z] Got ACK from engine");
                    break;

                case SsiOpcode.CMD_NAK:
                    HandleNak(data);
                    break;

                default:
                    Log?.Invoke($"[E200Z] Packet: Opcode=0x{(byte)opcode:X2}, Ext={extended}, DataLen={data.Length}");
                    break;
            }
        }

        private void HandleDecode(SsiOpcode opcode, byte[] data, bool extended)
        {
            if (data.Length < 1)
            {
                Log?.Invoke("[E200Z] Decode packet with no data");
                return;
            }

            byte barcodeType = data[0];
            byte[] textBytes = new byte[data.Length - 1];
            if (textBytes.Length > 0)
                Array.Copy(data, 1, textBytes, 0, textBytes.Length);

            // 한글 대응: 장치가 EUC-KR로 보내는 경우가 있으므로 euc-kr 사용
            string text = Encoding.GetEncoding("euc-kr").GetString(textBytes);

            var msg = new DecodeMessage
            {
                IsExtended = extended,
                BarcodeType = barcodeType,
                Text = text,
                RawData = textBytes
            };

            Log?.Invoke($"[E200Z] DECODE: Type=0x{barcodeType:X2}, Text=\"{text}\"");
            Decoded?.Invoke(this, msg);
        }

        private void HandleRevisionReply(byte[] data)
        {
            string rev = Encoding.ASCII.GetString(data);
            _lastRevision = rev;
            Log?.Invoke($"[E200Z] Revision: {rev}");
        }

        private void HandleNak(byte[] data)
        {
            byte cause = data.Length > 0 ? data[0] : (byte)0xFF;
            Log?.Invoke($"[E200Z] NAK received. Cause=0x{cause:X2}");
        }

        private async Task SendAckAsync(byte source)
        {
            try
            {
                var pkt = new SsiPacket
                {
                    Length = 0x04,
                    Opcode = (byte)SsiOpcode.CMD_ACK,
                    Source = 0x04, // Host
                    Status = 0x00,
                    Data = Array.Empty<byte>()
                };

                await SendPacketAsync(pkt, CancellationToken.None).ConfigureAwait(false);
                Log?.Invoke("[E200Z] TX: CMD_ACK sent");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[E200Z] SendAck error: {ex.Message}");
            }
        }

        public override ValueTask DisposeAsync()
        {
            try { _rxCts?.Cancel(); } catch { }
            try { if (_rxTask != null) _rxTask.Wait(500); } catch { }
            _rxCts = null;
            _rxTask = null;
            _transport = null;
            return base.DisposeAsync();
        }

        #endregion
    }

    #region Packet 정의
    /// <summary>
    /// SSI Opcode 정의 (필요한 것만).
    /// </summary>
    public enum SsiOpcode : byte
    {
        CMD_ACK = 0xD0,
        CMD_NAK = 0xD1,
        DECODE_DATA = 0xF3, // 1D
        DECODE_DATA_TWO = 0xF4, // 2D (QR 등, Extended 가능)
        LED_ON = 0xE7,
        LED_OFF = 0xE8,
        SCAN_ENABLE = 0xE9,
        SCAN_DISABLE = 0xEA,
        SLEEP = 0xEB,
        START_DECODE = 0xE4,
        STOP_DECODE = 0xE5,
        REQUEST_REVISION = 0xA3,
        REPLY_REVISION = 0xA4,
        RESET = 0xFA,
        CFG_PARAM_SEND = 0xC6 // 설정값 변경
    }

    /// <summary>
    /// SSI 기본 패킷 (STD 형식).
    /// Length &lt;= 255 (Extended 아닌 경우).
    /// </summary>
    public sealed class SsiPacket
    {
        public byte Length { get; set; }  // 체크섬 제외 전체 길이
        public byte Opcode { get; set; }
        public byte Source { get; set; }  // 0x04 = Host, 0x00 = Engine
        public byte Status { get; set; }  // Bit3: Change Type (영구 저장), Bit0: Retransmit
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public ushort Checksum { get; set; }

        public byte[] ToBytes()
        {
            var list = new List<byte>(Length + 2);
            list.Add(Length);
            list.Add(Opcode);
            list.Add(Source);
            list.Add(Status);

            if (Data is { Length: > 0 })
                list.AddRange(Data);

            ushort checksum = ComputeChecksum(list.ToArray(), 0, list.Count);
            Checksum = checksum;

            list.Add((byte)(checksum >> 8));   // High
            list.Add((byte)(checksum & 0xFF)); // Low

            return list.ToArray();
        }

        public static SsiPacket CreateSimple(SsiOpcode opcode)
        {
            return new SsiPacket
            {
                Length = 0x04,
                Opcode = (byte)opcode,
                Source = 0x04,
                Status = 0x00,
                Data = Array.Empty<byte>()
            };
        }

        /// <summary>
        /// 파라미터 설정용 패킷 (Group=0x00, Param=param, Value=value).
        /// Status bit3 = 1이면 플래시에 저장(영구).
        /// </summary>
        public static SsiPacket CreateParamByte(byte param, byte value, bool saveToFlash = true)
        {
            byte status = saveToFlash ? (byte)0x08 : (byte)0x00;
            byte[] data = new byte[] { 0x00, param, value }; // [Group=0x00, Param, Value]

            return new SsiPacket
            {
                Length = (byte)(4 + data.Length),
                Opcode = (byte)SsiOpcode.CFG_PARAM_SEND,
                Source = 0x04,
                Status = status,
                Data = data
            };
        }

        /// <summary>
        /// buffer[offset..offset+count-1] 합에 대한 16비트 2's complement.
        /// </summary>
        public static ushort ComputeChecksum(byte[] buffer, int offset, int count)
        {
            uint sum = 0;
            for (int i = 0; i < count; i++)
                sum += buffer[offset + i];

            return (ushort)(0 - sum);
        }
    }

    /// <summary>
    /// 디코드된 바코드 메시지.
    /// </summary>
    public sealed class DecodeMessage
    {
        public bool IsExtended { get; set; }
        public byte BarcodeType { get; set; }  // QR: 0xF1 (매뉴얼 기준)
        public string Text { get; set; } = "";
        public byte[] RawData { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// E200Z SSI 스캐너 드라이버.
    /// - DeviceBase + ITransport 사용.
    /// - 백그라운드 수신 루프에서 STD/EXT 패킷 모두 파싱.
    /// - Host Trigger, Auto-Induction 등은 PARAM_SEND 로 설정.
    /// </summary>
    #endregion
}
