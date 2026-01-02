using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using System.Diagnostics;
using System.IO;

namespace KIOSK.Devices.Drivers
{
    public sealed class DeviceHCDM10K : DeviceBase
    {
        private int _failThreshold;

        public DeviceHCDM10K(DeviceDescriptor desc, ITransport transport)
            : base(desc, transport)
        {
        }

        public async override Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                var transport = RequireTransport();

                if (transport.IsOpen)
                    await transport.CloseAsync(ct).ConfigureAwait(false);

                await transport.OpenAsync(ct).ConfigureAwait(false);
                _failThreshold = 0;

                if (transport.IsOpen)
                {
                    var res = await ExecuteAsync(new DeviceCommand("Init", Array.Empty<byte>()), ct);
                    await Task.Delay(10000);
                    return CreateSnapshot();
                }
                else
                {
                    return CreateSnapshot(new[]
                    {
                        CreateAlarm("00", "미연결", Severity.Error)
                    });
                }
            }
            catch (Exception)
            {
                _failThreshold++;
                return CreateSnapshot(new[]
                {
                    CreateAlarm("00", "미연결", Severity.Error)
                });
            }
        }

        public async override Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string temp = "")
        {
            var alarms = new List<DeviceAlarm>();

            try
            {
                var res = await ExecuteAsync(new DeviceCommand("Sensor", Array.Empty<byte>()), ct);

                if (res.Success)
                {
                    byte[] x = (byte[])res.Data;

                    if (x != null)
                    {
                        int index = 3;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "리젝트 박스 열림", Severity.Warning));

                        index++;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "방출 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 1)) == 1)
                            alarms.Add(CreateAlarm("01", "회수 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 2)) == 1)
                            alarms.Add(CreateAlarm("01", "GATE1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 3)) == 1)
                            alarms.Add(CreateAlarm("01", "GATE2 센서 감지", Severity.Warning));

                        index++;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트1 SKEW1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 1)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 2)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트1 시재 부족 감지", Severity.Warning));
                        if ((x[index] & (1 << 3)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트1 미장착 감지", Severity.Warning));
                        if ((x[index] & (1 << 4)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트1 딥스위치1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 5)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트1 딥스위치2 센서 감지", Severity.Warning));

                        index++;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트2 SKEW1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 1)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트2 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 2)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트2 시재 부족 감지", Severity.Warning));
                        if ((x[index] & (1 << 3)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트2 미장착 감지", Severity.Warning));
                        if ((x[index] & (1 << 4)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트2 딥스위치1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 5)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트2 딥스위치2 센서 감지", Severity.Warning));

                        index++;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트3 SKEW1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 1)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트3 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 2)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트3 시재 부족 감지", Severity.Warning));
                        if ((x[index] & (1 << 3)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트3 미장착 감지", Severity.Warning));
                        if ((x[index] & (1 << 4)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트3 딥스위치1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 5)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트3 딥스위치2 센서 감지", Severity.Warning));

                        index++;
                        if ((x[index] & (1 << 0)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트4 SKEW1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 1)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트4 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 2)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트4 시재 부족 감지", Severity.Warning));
                        if ((x[index] & (1 << 3)) == 0)
                            alarms.Add(CreateAlarm("01", "카세트4 미장착 감지", Severity.Warning));
                        if ((x[index] & (1 << 4)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트4 딥스위치1 센서 감지", Severity.Warning));
                        if ((x[index] & (1 << 5)) == 1)
                            alarms.Add(CreateAlarm("01", "카세트4 딥스위치2 센서 감지", Severity.Warning));
                    }
                    _failThreshold = 0;

                    Trace.WriteLine(res.Message.ToString());
                }
                else
                {
                    _failThreshold++;
                }
            }
            catch (IOException)
            {
                _failThreshold++;
            }
            catch (TimeoutException)
            {
                _failThreshold++;
            }
            catch (Exception)
            {
                _failThreshold++;
            }

            if (_failThreshold > 5)
                alarms.Add(CreateAlarm("01", "통신오류", Severity.Warning));

            return CreateSnapshot(alarms);
        }

        public async override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            try
            {
                using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
                switch (command)
                {
                    // TODO: 각 커맨드 정리 필요
                    // init - response init(parse) 결과 리턴
                    // dispense - response dispense 결과 리턴 (성공/실패, 몇장..)
                    case { Name: string name, Payload: byte[] data } when name.Equals("SENSOR", StringComparison.OrdinalIgnoreCase):
                        {
                            var res = await SendCommandAsync('S', Array.Empty<byte>(), processTimeoutMs: 5000, ct);
                            return res is null
                                ? new CommandResult(false)
                                : new CommandResult(true, Data: res);
                        }

                    case { Name: string name, Payload: byte[] data } when name.Equals("INIT", StringComparison.OrdinalIgnoreCase):
                        {
                            var res = await SendCommandAsync('I', new byte[] { 0x00 }, processTimeoutMs: 30000, ct);
                            return res is null
                                ? new CommandResult(false)
                                : new CommandResult(true, Data: res);
                        }

                    case { Name: string name, Payload: byte[] data } when name.Equals("DISPENSE", StringComparison.OrdinalIgnoreCase):
                        {
                            var res = await SendCommandAsync('D', data, processTimeoutMs: 120000, ct);
                            return res is null
                                ? new CommandResult(false)
                                : new CommandResult(true, Data: res);
                        }
                    case { Name: string name, Payload: byte[] data } when name.Equals("EJECT", StringComparison.OrdinalIgnoreCase):
                        {
                            var res = await SendCommandAsync('F', data, processTimeoutMs: 10000, ct);
                            return res is null
                                ? new CommandResult(false)
                                : new CommandResult(true, Data: res);
                        }
                    default:
                        return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
                }
            }
            catch (OperationCanceledException)
            {
                return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
            }
        }

        #region Protocol
        public record Response(bool result, byte[] Data);

        const byte STX = 0x02;
        const byte ETX = 0x03;
        const byte ENQ = 0x05;
        const byte ACK = 0x06;
        const byte NAK = 0x15;

        const int ACK_WAIT_MS = 500;
        const int MAX_ENQ = 10;
        const int MAX_NAK = 3;

        public byte[] BuildFrame(char cmd, byte[]? data = null)
        {
            data ??= Array.Empty<byte>();
            int len = 1 + data.Length; // CMD + DATA
            byte lenL = (byte)(len & 0xff);
            byte lenH = (byte)((len >> 8) & 0xff);

            // STX LENL LENH CMD DATA ETX CHK
            var buf = new byte[1 + 2 + len + 1 + 1];
            int idx = 0;
            buf[idx++] = STX;
            buf[idx++] = lenL;
            buf[idx++] = lenH;
            buf[idx++] = (byte)cmd;
            if (data.Length > 0) { Buffer.BlockCopy(data, 0, buf, idx, data.Length); idx += data.Length; }
            buf[idx++] = ETX;
            byte cs = 0;
            for (int i = 1; i < idx; i++) cs ^= buf[i];
            buf[idx++] = cs;
            return buf;
        }

        public (bool ok, string err, byte[] data)? ParseResponse(ReadOnlySpan<byte> frame)
        {
            // 최소: STX LENL LENH CMD ETX CHK
            if (frame.Length < 1 + 1 + 1 + 1 + 1 + 1) return null;
            if (frame[0] != STX) return null;


            int len = frame[1] | (frame[2] << 8);
            int expected = 1 + 2 + len + 1 + 1;
            if (frame.Length < expected) return null;

            // ETX 위치: 뒤에서 2바이트 앞(ETX, CHK)
            int etxPos = frame.Length - 2;
            if (frame[etxPos] != ETX) return null;

            // CHK 검사
            byte cs = 0;
            for (int i = 1; i <= etxPos; i++) cs ^= frame[i];
            if (cs != frame[etxPos + 1]) return null;

            char res = (char)frame[3];
            var data = frame.Slice(4, len - 1).ToArray();

            bool ok = res == 0x4F;
            return (ok, string.Empty, data);
        }

        public async Task<byte[]?> SendCommandAsync(
            char cmd,
            byte[]? data = null,
            int processTimeoutMs = 5000,
            CancellationToken ct = default)
        {
            var _transport = RequireTransport();
            if (!_transport.IsOpen) return null;

            var frame = BuildFrame(cmd, data);
            Trace.WriteLine($"[HCDM] Send: {BitConverter.ToString(frame)}");
            await _transport.WriteAsync(frame, ct);

            // ACK 대기, ENQ 재시도
            if (!await WaitAckWithEnqAsync(ct)) return null;

            int timeout = processTimeoutMs;
            int nak = 0;
            while (true)
            {
                var full = await ReadFullFrameWithTimeoutAsync(_transport, timeout, 256, ct);
                Trace.WriteLine($"[HCDM] Recv: {BitConverter.ToString(full ?? Array.Empty<byte>())}");
                if (full == null) return null;
                var parsed = ParseResponse(full);
                if (parsed != null)
                {
                    await _transport.WriteAsync(new byte[] { ACK }, ct);  // 정상 응답에는 ACK
                    return parsed.Value.data;
                }
                nak++;
                if (nak > MAX_NAK) return null;
                await _transport.WriteAsync(new byte[] { NAK }, ct);
                await Task.Delay(50, ct);
            }
        }

        async Task<int> TryReadByteAsync(int timeoutMs, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            var _transport = RequireTransport();

            var buf = new byte[1];
            try
            {
                int r = await _transport.ReadAsync(buf, cts.Token);
                if (r <= 0) return -1;
                return buf[0];
            }
            catch
            {
                return -1;
            }
        }

        async Task<bool> WaitAckWithEnqAsync(CancellationToken ct)
        {
            var _transport = RequireTransport();

            // 우선 바로 확인
            if (await TryReadByteAsync(ACK_WAIT_MS, ct) == ACK)
            {
                Trace.WriteLine("ACK");
                return true;
            }

            for (int i = 0; i < MAX_ENQ; i++)
            {
                Trace.WriteLine("ENQ");
                await _transport.WriteAsync(new byte[] { ENQ }, ct);
                if (await TryReadByteAsync(ACK_WAIT_MS, ct) == ACK) return true;
            }
            return false;
        }

        // STX(1) + LEN(2) + (len bytes) + CHK(1)
        async Task<byte[]?> ReadFullFrameWithTimeoutAsync(
            ITransport transport,
            int timeoutMs,
            int maxLen = 256,
            CancellationToken ct = default)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            // 로컬 헬퍼: 일정 ms 만큼만 기다리며 정확히 n바이트를 채운다(남은 시간 예산 사용)
            static async Task<bool> ReadExactUntilAsync(
                ITransport t, byte[] buf, int offset, int count, DateTime deadline, CancellationToken ct)
            {
                int got = 0;
                while (got < count)
                {
                    int remainMs = (int)Math.Max(1, (deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (remainMs <= 0) return false;

                    // 짧은 슬라이스로 폴링 (예: 50ms)
                    int slice = Math.Min(remainMs, 50);

                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(slice);
                        int r = await t.ReadAsync(buf.AsMemory(offset + got, count - got), cts.Token)
                                       .ConfigureAwait(false);
                        if (r > 0)
                        {
                            got += r;
                            continue;
                        }

                        // r==0 → 아직 데이터 없음: 계속 대기(루프 지속)
                        continue;
                    }
                    catch (TimeoutException)
                    {
                        // 슬라이스 타임아웃 → 계속 대기(루프 지속)
                        continue;
                    }
                }
                return true;
            }

            // 1) STX 탐색 (데드라인 내에서 폴링)
            byte stx = 0;
            var one = new byte[1];
            while (true)
            {
                int remainMs = (int)Math.Max(1, (deadline - DateTime.UtcNow).TotalMilliseconds);
                if (remainMs <= 0) return null;

                int slice = Math.Min(remainMs, 50);
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(slice);

                    int r = await transport.ReadAsync(one, cts.Token).ConfigureAwait(false);
                    if (r <= 0)
                    {
                        // 아직 안 옴 → 계속 폴링
                        continue;
                    }

                    if (one[0] == STX) { stx = STX; break; }
                    // STX가 아니면 버리고 계속
                }
                catch (TimeoutException ex)
                {
                    // 슬라이스 타임아웃 → 계속 폴링
                    continue;
                }
            }

            // 2) LENL, LENH
            var lenBuf = new byte[2];
            if (!await ReadExactUntilAsync(transport, lenBuf, 0, 2, deadline, ct).ConfigureAwait(false))
                return null;

            int len = lenBuf[0] | (lenBuf[1] << 8);
            if (len <= 0 || len > maxLen) return null;

            // 3) 본문 + ETX + CHK  => len(CMD+DATA) + 2
            var tail = new byte[len + 2];
            if (!await ReadExactUntilAsync(transport, tail, 0, tail.Length, deadline, ct).ConfigureAwait(false))
                return null;

            // 4) ETX/CHK 검사
            int etxPos = len;
            if (tail[etxPos] != ETX) return null;

            byte chk = tail[etxPos + 1];
            byte cs = 0;
            cs ^= lenBuf[0];
            cs ^= lenBuf[1];
            for (int i = 0; i < len; i++) cs ^= tail[i];
            cs ^= ETX;
            if (cs != chk) return null;

            // 5) full 프레임 합치기
            var full = new byte[3 + tail.Length];
            full[0] = STX;
            full[1] = lenBuf[0];
            full[2] = lenBuf[1];
            Buffer.BlockCopy(tail, 0, full, 3, tail.Length);
            return full;
        }

        #endregion
    }
}
