using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Devices.Drivers
{
    /// <summary>
    /// HCDM-20K (BCR 사양) 드라이버
    /// - RS-232 115200/8N1 가정
    /// - Frame: STX CMD DATA ETX CRC1 CRC2  (DATA는 ASCII 10진수/지정 포맷, CRC16-IBM)
    /// - Response: STX CMD ERR(2 ASCII hex) DATA ETX CRC1 CRC2
    /// - ACK(0x06), NAK(0x15), ENQ(0x05) 처리
    /// Spec ref: "BCR 프로토콜 사양서 I (2019.02.13)"
    /// </summary>
    public sealed class DeviceHCDM20K : DeviceBase
    {
        // Protocol bytes
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte ENQ = 0x05;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;

        // Timings
        private const int ACK_WAIT_MS = 300;   // 사양 예시에 맞춤(300ms)
        private const int MAX_NAK = 1;         // 사양상 재시도 최대 1회
        private const int RX_CHUNK_MAX = 2048; // 안전 버퍼

        public DeviceHCDM20K(DeviceDescriptor desc, ITransport transport)
            : base(desc, transport) { }

        #region Lifecycle
        public async override Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                var transport = RequireTransport();

                if (transport.IsOpen)
                    await transport.CloseAsync(ct).ConfigureAwait(false);

                await transport.OpenAsync(ct).ConfigureAwait(false);

                if (!transport.IsOpen)
                {
                    return CreateSnapshot(new[] { CreateAlarm("00", "미연결", Severity.Error) });
                }

                // Initialize 'T' : 5개 필드 (사양 2.1)
                // 1: OCR 미독 허용 문자열 수 '0'~'4' -> 기본 "0"
                // 2: 국가(0:한국,1:중국,2:미국) -> 필요에 맞게 설정. 기본 "0"
                // 3: 카세트 수량(최대 6) -> 현 시스템 카세트 수. 기본 "4"
                // 4: 위/변조감별(0/1/2) -> 기본 "1"
                // 5: 카세트 권종 인덱스(카세트 수만큼 1byte씩). 데모로 모두 '0'
                var cassetteCount = 4; // 필요 시 외부 구성에서 받아오도록 변경
                var initData = new List<string>
                {
                    "0",            // unread tolerance
                    "0",            // country: Korea
                    cassetteCount.ToString(), // cassette count
                    "0"             // anti-counterfeit check
                };
                // 권종 인덱스: 카세트 수만큼
                for (int i = 0; i < cassetteCount; i++) initData.Add("0");

                var initRes = await SendAsciiCommandAsync('T', initData, overallTimeoutMs: 8000, ct: ct);
                if (!initRes.success)
                {
                    return CreateSnapshot(new[] { CreateAlarm("T0", $"Init 실패: {initRes.message}", Severity.Error) });
                }

                return CreateSnapshot();
            }
            catch
            {
                return CreateSnapshot(new[] { CreateAlarm("00", "미연결", Severity.Error) });
            }
        }

        public async override Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default)
        {
            var alarms = new List<DeviceAlarm>();

            try
            {
                var res = await SendAsciiCommandAsync('S', null, overallTimeoutMs: 2000, ct: ct);
                if (!res.success)
                {
                    alarms.Add(CreateAlarm("01", "통신오류", Severity.Warning));
                }
                else
                {
                    var data = res.data ?? Array.Empty<byte>();
                    if (data.Length >= 16)
                    {
                        // 예시 알람 매핑
                        if (BitIsSet(data[0], 5)) alarms.Add(CreateAlarm("RB", "6번 카세트 용지 부족", Severity.Warning));
                        if (BitIsSet(data[0], 4)) alarms.Add(CreateAlarm("RB", "5번 카세트 용지 부족", Severity.Warning));
                        if (BitIsSet(data[0], 3)) alarms.Add(CreateAlarm("RB", "4번 카세트 용지 부족", Severity.Warning));
                        if (BitIsSet(data[0], 2)) alarms.Add(CreateAlarm("RB", "3번 카세트 용지 부족", Severity.Warning));
                        if (BitIsSet(data[0], 1)) alarms.Add(CreateAlarm("RB", "2번 카세트 용지 부족", Severity.Warning));
                        if (BitIsSet(data[0], 0)) alarms.Add(CreateAlarm("RB", "1번 카세트 용지 부족", Severity.Warning));

                        if (BitIsSet(data[3], 5)) alarms.Add(CreateAlarm("RB", "6번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[3], 4)) alarms.Add(CreateAlarm("RB", "5번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[3], 3)) alarms.Add(CreateAlarm("RB", "4번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[3], 2)) alarms.Add(CreateAlarm("RB", "3번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[3], 1)) alarms.Add(CreateAlarm("RB", "2번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[3], 0)) alarms.Add(CreateAlarm("RB", "1번 SKEW1 센서 감지", Severity.Warning));

                        if (BitIsSet(data[4], 5)) alarms.Add(CreateAlarm("RB", "6번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[4], 4)) alarms.Add(CreateAlarm("RB", "5번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[4], 3)) alarms.Add(CreateAlarm("RB", "4번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[4], 2)) alarms.Add(CreateAlarm("RB", "3번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[4], 1)) alarms.Add(CreateAlarm("RB", "2번 SKEW1 센서 감지", Severity.Warning));
                        if (BitIsSet(data[4], 0)) alarms.Add(CreateAlarm("RB", "1번 SKEW1 센서 감지", Severity.Warning));

                        if (data[6] == '1') alarms.Add(CreateAlarm("RB", "GATE1 경로 감지", Severity.Warning)); // Byte12 bit0
                        if (data[7] == '1') alarms.Add(CreateAlarm("RB", "GATE2 경로 감지", Severity.Warning)); // Byte12 bit0
                        //if (data[8] == '1') alarms.Add(CreateAlarm("RB", "GATE1 경로 감지", Severity.Warning)); // Byte12 bit0
                        if (data[9] == '1') alarms.Add(CreateAlarm("RB", "EXIT1 경로 감지", Severity.Warning)); // Byte12 bit0
                        if (data[10] == '1') alarms.Add(CreateAlarm("RB", "REJECT IN 경로 감지", Severity.Warning)); // Byte12 bit0
                        if (data[11] == '1') alarms.Add(CreateAlarm("RB", "REJECT BOX 열림", Severity.Warning)); // Byte12 bit0
                        if (data[12] == '1') alarms.Add(CreateAlarm("RB", "CISssss BOX 열림", Severity.Warning)); // Byte12 bit0
                    }
                }
            }
            catch
            {
                alarms.Add(CreateAlarm("01", "통신오류", Severity.Warning));
            }

            return CreateSnapshot(alarms);
        }
        #endregion

        #region Commands
        public async override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

            try
            {
                switch (command)
                {
                    case { Name: string name } when name.Equals("SENSOR", StringComparison.OrdinalIgnoreCase):
                        {
                            var r = await SendAsciiCommandAsync('S', null, overallTimeoutMs: 2000, ct: ct);
                            return r.success ? new CommandResult(true, Data: r.data) : new CommandResult(false, r.message);
                        }
                    case { Name: string name, Payload: byte[] data } when name.Equals("INIT", StringComparison.OrdinalIgnoreCase):
                        {
                            // 외부에서 'T' 데이터 지정 시: data에는 ASCII 바이트들이 들어있다고 가정
                            var r = await SendAsciiCommandRawAsync('T', data, overallTimeoutMs: 8000, ct: ct);
                            return r.success ? new CommandResult(true, Data: r.data) : new CommandResult(false, r.message);
                        }
                    case { Name: string name } when name.Equals("VERSION", StringComparison.OrdinalIgnoreCase):
                        {
                            var r = await SendAsciiCommandAsync('V', null, overallTimeoutMs: 2000, ct: ct);
                            return r.success ? new CommandResult(true, Data: r.data) : new CommandResult(false, r.message);
                        }
                    case { Name: string name, Payload: byte[] data } when name.Equals("EJECT", StringComparison.OrdinalIgnoreCase):
                        {
                            // 'J' BackFeed 재시도 횟수(최대 1회) -> 기본 "0"
                            var args = new[] { (data != null && data.Length > 0) ? Encoding.ASCII.GetString(data) : "0" };
                            var r = await SendAsciiCommandAsync('J', args, overallTimeoutMs: 5000, ct: ct);
                            return r.success ? new CommandResult(true, Data: r.data) : new CommandResult(false, r.message);
                        }
                    case { Name: string name, Payload: byte[] data } when name.Equals("DISPENSE", StringComparison.OrdinalIgnoreCase):
                        {
                            // payload는 상위 레이어에서 “카세트 수량 + (카세트,요구매수[3자리]) 반복”의 ASCII로 만들어 주는 것도 가능
                            // 혹은 간단한 DTO를 정의해 여기서 ASCII로 직렬화하도록 바꿔도 됨.
                            // 타임아웃: (총 요구매수/3 + 5)초 + ENQ마다 +3초 (사양 2.6)
                            // 여기서는 payload에서 총 요구매수를 추정하거나 상위에서 overallTimeoutMs를 넘겨주도록 설계 가능.
                            int estimatedCount = EstimateTotalRequestedFromPayload(data);
                            int timeoutMs = (int)((estimatedCount / 3.0 + 5) * 1000);

                            var r = await SendAsciiCommandRawAsync('D', data, overallTimeoutMs: Math.Max(timeoutMs, 15000), ct: ct, isLongOpWithEnq: true);
                            return r.success ? new CommandResult(true, Data: r.data) : new CommandResult(false, r.message);
                        }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
            }

            return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
        }
        #endregion

        #region ASCII/CRC16 Protocol Helpers
        private static bool BitIsSet(byte b, int bit) => ((b >> bit) & 0x01) == 0x01;

        static ushort Crc16IbM(byte[] data, int offset, int count)
        {
            ushort crc = 0xFFFF;              // init
            for (int i = 0; i < count; i++)
            {
                crc ^= (ushort)(data[offset + i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;                        // xorout 0x0000
        }

        private static byte[] BuildFrame(char cmd, byte[] data = null)
        {
            // STX CMD DATA ETX CRC1 CRC2
            int len = 1 + data.Length + 1 + 2;
            var buf = new byte[1 + len];
            int idx = 0;
            buf[idx++] = STX;
            buf[idx++] = (byte)cmd;
            if (data.Length > 0) { Buffer.BlockCopy(data, 0, buf, idx, data.Length); idx += data.Length; }
            buf[idx++] = ETX;

            // CRC16 over bytes including STX..ETX (사양 명시)
            ushort crc = Crc16IbM(buf, 0, idx);
            // CRC1=상위8bit, CRC2=하위8bit
            buf[idx++] = (byte)((crc >> 8) & 0xFF);
            buf[idx++] = (byte)(crc & 0xFF);
            return buf;
        }

        private static (bool ok, char cmd, string err, byte[] data)? ParseResponse(ReadOnlySpan<byte> frame)
        {
            // 최소: STX CMD ERR(2) ETX CRC1 CRC2
            if (frame.Length < 1 + 1 + 2 + 1 + 2) return null;
            if (frame[0] != STX) return null;

            // ETX 위치: 뒤에서 3바이트 앞(ETX, CRC1, CRC2)
            int etxPos = frame.Length - 3;
            if (etxPos < 3) return null;
            if (frame[etxPos] != ETX) return null;

            // CRC 검사
            ushort expectedCrc = (ushort)((frame[^2] << 8) | frame[^1]);
            ushort calc = Crc16IbM(frame.ToArray(), 0, frame.Length - 2);
            if (expectedCrc != calc) return null;

            char cmd = (char)frame[1];
            string err = Encoding.ASCII.GetString(frame.Slice(2, 2));
            var data = frame.Slice(4, etxPos - 4).ToArray(); // CMD(1)+ERR(2) 이후 ~ ETX 전까지

            bool ok = string.Equals(err, "00", StringComparison.OrdinalIgnoreCase);
            return (ok, cmd, err, data);
        }

        private async Task<(bool success, string message, byte[]? data)> SendAsciiCommandAsync(
            char cmd,
            IEnumerable<string>? fields,
            int overallTimeoutMs,
            CancellationToken ct)
        {
            byte[] payload = fields is null ? Array.Empty<byte>() : Encoding.ASCII.GetBytes(string.Concat(fields));
            return await SendAsciiCommandRawAsync(cmd, payload, overallTimeoutMs, ct);
        }

        /// <summary>
        /// 20K 전용 전송: ACK 대기 → (장시간 작업 시 ENQ 수신하며 연장) → 최종 응답 수신 → ACK 회신
        /// </summary>
        private async Task<(bool success, string message, byte[]? data)> SendAsciiCommandRawAsync(
            char cmd,
            byte[]? asciiPayload,
            int overallTimeoutMs,
            CancellationToken ct,
            bool isLongOpWithEnq = false)
        {
            var transport = RequireTransport();
            if (!transport.IsOpen) return (false, "NotOpen", null);

            var frame = BuildFrame(cmd, asciiPayload ?? Array.Empty<byte>());
            Trace.WriteLine($"[HCDM20K] TX: {BitConverter.ToString(frame)}");
            await transport.WriteAsync(frame, ct).ConfigureAwait(false);

            // 1) ACK/NAK 대기 (최대 1회 재전송 허용)
            for (int attempt = 0; attempt <= MAX_NAK; attempt++)
            {
                int ack = await TryReadByteAsync(ACK_WAIT_MS, ct).ConfigureAwait(false);
                if (ack == ACK) break;
                if (ack == NAK)
                {
                    Trace.WriteLine("[HCDM20K] NAK, retry send");
                    await transport.WriteAsync(frame, ct).ConfigureAwait(false);
                    continue;
                }
                if (ack < 0) // timeout
                {
                    Trace.WriteLine("[HCDM20K] ACK timeout, retry send");
                    await transport.WriteAsync(frame, ct).ConfigureAwait(false);
                    continue;
                }
                // 기타 바이트가 오면 무시하고 루프 지속
                attempt--; // 예외 상황: 재시도 카운트 소모하지 않음
            }

            // 2) 처리/응답 수신
            var deadline = Stopwatch.StartNew();
            int extendMs = overallTimeoutMs;

            while (true)
            {
                // ENQ 또는 RESP 프레임을 판별
                int b = await TryReadByteAsync(Math.Max(1, extendMs - (int)deadline.ElapsedMilliseconds), ct).ConfigureAwait(false);
                if (b < 0)
                {
                    return (false, "Timeout waiting response", null);
                }

                if (b == ENQ)
                {
                    // ENQ 수신 시: 3초 연장 (사양 2.6)
                    extendMs += 3000;
                    Trace.WriteLine("[HCDM20K] ENQ received (+3s)");
                    continue;
                }

                if (b == STX)
                {
                    // 나머지 바디를 버퍼링: CMD, … , ETX, CRC1, CRC2
                    var resp = await ReadUntilEtXAndCrcAsync(transport, firstByteWasStx: true, ct: ct).ConfigureAwait(false);
                    Trace.WriteLine($"[HCDM20K] RX: {BitConverter.ToString(resp ?? Array.Empty<byte>())}");
                    if (resp == null) return (false, "Malformed response", null);

                    var parsed = ParseResponse(resp);
                    if (parsed == null) return (false, "CRC/format error", null);

                    // 정상/오류 판단
                    // 정상·비정상 모두 최종 응답에는 ACK 전송 (사양 플로우)
                    await transport.WriteAsync(new byte[] { ACK }, ct).ConfigureAwait(false);

                    if (parsed.Value.data != null)
                        return (true, $"ERR={parsed.Value.err}", parsed.Value.data);
                    else
                        return (false, $"ERR={parsed.Value.err}", parsed.Value.data);
                }

                // 잡신호 무시
            }
        }

        private async Task<int> TryReadByteAsync(int timeoutMs, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            var t = RequireTransport();
            var buf = new byte[1];
            try
            {
                int r = await t.ReadAsync(buf, cts.Token).ConfigureAwait(false);

                if (r <= 0) return -1;
                return buf[0];
            }
            catch { return -1; }
        }

        private async Task<byte[]?> ReadUntilEtXAndCrcAsync(ITransport transport, bool firstByteWasStx, CancellationToken ct)
        {
            // STX는 이미 읽었으므로 버퍼에 포함해서 반환
            using var ms = new MemoryStream();
            if (firstByteWasStx) ms.WriteByte(STX);

            var tmp = new byte[64];
            // ETX를 찾고 ETX 뒤 CRC2바이트까지 모을 때까지 읽기
            bool sawEtx = false;
            int tailNeeded = 2; // CRC1+CRC2
            while (ms.Length < RX_CHUNK_MAX)
            {
                int r = await transport.ReadAsync(tmp, ct).ConfigureAwait(false);
                if (r <= 0) return null;
                ms.Write(tmp, 0, r);

                var arr = ms.ToArray();
                if (!sawEtx)
                {
                    int etxPos = Array.IndexOf(arr, ETX, 2); // CMD 이후로만 탐색
                    if (etxPos >= 0)
                    {
                        sawEtx = true;
                        // ETX 뒤 CRC 2바이트 남았는지 확인
                        if (arr.Length >= etxPos + 1 + tailNeeded) break;
                    }
                }
                else
                {
                    // 이미 ETX 봤다면 CRC까지 찼는지 체크
                    int etxPos = Array.LastIndexOf(arr, ETX);
                    if (arr.Length >= etxPos + 1 + tailNeeded) break;
                }
            }
            return ms.ToArray();
        }
        #endregion

        #region Helpers
        private static int EstimateTotalRequestedFromPayload(byte[] payload)
        {
            // payload가 "N (cassette,count3)..." 구조의 ASCII라고 가정
            // 대충 3자리 count를 모두 더함.
            if (payload == null || payload.Length == 0) return 0;
            try
            {
                string s = Encoding.ASCII.GetString(payload);
                if (s.Length == 0) return 0;

                int i = 0;
                int total = 0;

                // 첫 글자: 방출 카세트 수량 (1byte ASCII)
                if (i < s.Length && char.IsDigit(s[i]))
                {
                    int n = s[i] - '0';
                    i++;
                    for (int k = 0; k < n; k++)
                    {
                        if (i + 4 <= s.Length) // cassette(1) + count(3)
                        {
                            // 1바이트 카세트 인덱스는 그냥 건너뛰고
                            i += 1;
                            // 3자리 count
                            if (int.TryParse(s.AsSpan(i, Math.Min(3, s.Length - i)), out int c))
                                total += c;
                            i += 3;
                        }
                    }
                }
                return total;
            }
            catch { return 0; }
        }
        #endregion
    }
}
