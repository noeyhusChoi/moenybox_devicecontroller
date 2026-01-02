using KIOSK.Device.Abstractions;
using System.IO;

namespace KIOSK.Device.Drivers;

public sealed class DevicePrinter : DeviceBase
{
    private int _failThreshold;

    public DevicePrinter(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            var transport = RequireTransport();

            if (transport.IsOpen)
                await transport.CloseAsync(ct).ConfigureAwait(false);

            await transport.OpenAsync(ct).ConfigureAwait(false);
            _failThreshold = 0;

            return CreateSnapshot();
        }
        catch (Exception)
        {
            _failThreshold++;
            return CreateSnapshot(new[]
            {
                CreateAlarm("PRINT", "미연결")
            });
        }
    }

    public override async Task<DeviceStatusSnapshot> GetStatusAsync(
        CancellationToken ct = default,
        string temp = "임시메소드")
    {
        var alarms = new List<DeviceAlarm>();

        try
        {
            using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
            var transport = RequireTransport();

            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);

            byte[] cmd = { 0x1D, 0x72, 0x01 };
            await transport.WriteAsync(cmd, ct).ConfigureAwait(false);

            await Task.Delay(1000, ct).ConfigureAwait(false);

            byte[] resp = new byte[8];
            int read = await transport.ReadAsync(resp, ct).ConfigureAwait(false);

            if (read > 0)
            {
                _failThreshold = 0;

                if ((resp[0] & 0x01) != 0) alarms.Add(CreateAlarm("PRINT", "용지 없음", Severity.Warning));
                if ((resp[0] & 0x02) != 0) alarms.Add(CreateAlarm("PRINT", "헤드 업", Severity.Warning));
                if ((resp[0] & 0x04) != 0) alarms.Add(CreateAlarm("PRINT", "용지 에러 있음", Severity.Warning));
                if ((resp[0] & 0x08) != 0) alarms.Add(CreateAlarm("PRINT", "용지 잔량 적음", Severity.Warning));
                if ((resp[0] & 0x10) != 0) alarms.Add(CreateAlarm("PRINT", "프린트 진행중", Severity.Info));
                if ((resp[0] & 0x20) != 0) alarms.Add(CreateAlarm("PRINT", "커터 에러 있음", Severity.Warning));
                if ((resp[0] & 0x80) != 0) alarms.Add(CreateAlarm("PRINT", "보조 센서 용지 있음", Severity.Warning));
            }
            else
            {
                _failThreshold++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            _failThreshold++;
            throw;
        }
        catch (Exception)
        {
            _failThreshold++;
            throw;
        }

        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("PRINT", "응답 없음", Severity.Warning));

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            switch (command)
            {
                case { Name: string name, Payload: string data } when name.Equals("PRINTCONTENT", StringComparison.OrdinalIgnoreCase):
                    {
                        await LmarginMm(3.5);
                        await setAlign(0);
                        await textStyle(0, 0, 0, 0);
                        return await printStr(data);
                    }

                case { Name: string name, Payload: string data } when name.Equals("PRINTTITLE", StringComparison.OrdinalIgnoreCase):
                    {
                        await LmarginMm(3.5);
                        await setAlign(1);
                        await textStyle(0, 1, 0, 1);
                        return await printStr(data);
                    }

                case { Name: string name } when name.Equals("CUT", StringComparison.OrdinalIgnoreCase):
                    {
                        return await cut();
                    }

                case { Name: string name, Payload: string data } when name.Equals("QR", StringComparison.OrdinalIgnoreCase):
                    {
                        byte type = data.Length switch
                        {
                            <= 18 => 1,
                            <= 54 => 3,
                            <= 106 => 5,
                            <= 230 => 9,
                            _ => 9
                        };

                        await LmarginMm(3.5);
                        await setAlign(1);
                        return await qrCode(data, type);
                    }

                case { Name: string name, Payload: int data } when name.Equals("ALIGN", StringComparison.OrdinalIgnoreCase):
                    {
                        await LmarginMm(3.5);
                        return await setAlign(data);
                    }

                default:
                    return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
            }
        }
        catch (OperationCanceledException)
        {
            return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
        }
        catch (IOException ex)
        {
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
        }
    }

    #region Protocol
    /// <summary>
    /// 텍스트 정렬
    /// </summary>
    /// <param name="align"> 0:왼쪽 1:가운데 2:오른쪽 정렬</param>
    public async Task<CommandResult> setAlign(int align)
    {
        var transport = RequireTransport();

        byte value = 0;
        switch (align)
        {
            case 0: // Left
                value = 0x00;
                break;
            case 1: // Center
                value = 0x01;
                break;
            case 2: // Right
                value = 0x02;
                break;
        }

        try
        {
            byte[] buf = new byte[] { 0x1b, 0x61, value };

            await transport.WriteAsync(buf);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// 인쇄지 컷팅
    /// </summary>
    /// /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> cut()
    {
        var transport = RequireTransport();

        try
        {
            byte[] buf = new byte[] { 0x1B, 0x69 };

            await transport.WriteAsync(buf);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// 텍스트 스타일
    /// </summary>
    /// <param name="width"> 0:가로확대해제 1:가로확대지정</param>
    /// <param name="height"> 0:세로확대해제 1:세로확대지정</param>
    /// <param name="under">0:밑줄해제 1:밑줄지정</param>
    /// <param name="bold">0:강조해제 1:강조지정</param>
    /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> textStyle(int width, int height, int under, int bold)
    {
        var transport = RequireTransport();
        try
        {
            // global style setting
            byte styleFlags1 = 0;
            if (bold == 1) styleFlags1 |= (1 << 3);      // Bit 3 for bold
            if (height == 1) styleFlags1 |= (1 << 4);    // Bit 4 for height
            if (width == 1) styleFlags1 |= (1 << 5);     // Bit 5 for width
            if (under == 1) styleFlags1 |= (1 << 7);     // Bit 7 for underline

            // korean style setting (한글은 추가 처리 필요)
            byte styleFlags2 = 0;
            if (width == 1) styleFlags2 |= (1 << 2);     // Bit 2 for width
            if (height == 1) styleFlags2 |= (1 << 3);    // Bit 3 for height
            if (under == 1) styleFlags2 |= (1 << 7);     // Bit 7 for underline


            await transport.WriteAsync(new byte[] { 0x1b, 0x21, styleFlags1 });
            await transport.WriteAsync(new byte[] { 0x1c, 0x21, styleFlags2 });

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// 텍스트 프린트
    /// </summary>
    /// /// <param name="data">프린트할 데이터</param>
    /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> printStr(string data)
    {
        var transport = RequireTransport();

        try
        {
            byte[] buf = System.Text.Encoding.GetEncoding("ks_c_5601-1987").GetBytes(data);

            await transport.WriteAsync(buf);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// 왼쪽 여백 설정
    /// </summary>
    /// <param name="margin"> margin * 0.125mm </param>
    /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> LmarginMm(double mm)
    {
        var transport = RequireTransport();

        try
        {
            if (mm < 0) mm = 0;
            int units = (int)Math.Round(mm / 0.125); // 1 unit = 0.125 mm
            if (units > 65535) units = 65535;

            byte nL = (byte)(units & 0xFF);
            byte nH = (byte)((units >> 8) & 0xFF);

            byte[] buf = new byte[] { 0x1D, 0x4C, nL, nH };

            await transport.WriteAsync(buf);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }


    public async Task<CommandResult> setAutoGetStatus()
    {
        var transport = RequireTransport();

        try
        {
            byte[] buf = new byte[] { 0x1d, 0x61, 0x00 };

            await transport.WriteAsync(buf);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }


    /// <summary>
    /// 프린터 상태 확인 패킷 전송
    /// </summary>
    /// <returns>성공: 상태정보 바이트, 실패: -1</returns>
    public async Task<CommandResult> printerStatus()
    {
        var transport = RequireTransport();

        try
        {
            byte[] buf = new byte[] { 0x1d, 0x72, 0x01 };
            await transport.WriteAsync(buf);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource();
            timeoutCts.CancelAfter(1000);

            // 프린터 응답 수신
            byte[] read = new byte[256];
            int response = await transport.ReadAsync(read, timeoutCts.Token).ConfigureAwait(false);

            return response > 0
                ? new CommandResult(true, "", read)
                : new CommandResult(false);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// QR 코드 프린트
    /// </summary>
    /// <param name="data">QR 코드에 넣을 데이터 (버전별 지원 사이즈 다름)</param>
    /// <param name="version">QR 코드 버전(1, 3, 5, 9만 지원)</param>
    /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> qrCode(string data, int version)
    {
        var transport = RequireTransport();

        if (string.IsNullOrEmpty(data))
            return new CommandResult(false, "Invalid Data");

        // 인코딩 바이트 길이로 체크 (특수문자 포함 대비)
        byte[] buf = System.Text.Encoding.GetEncoding("ks_c_5601-1987").GetBytes(data);

        // 지원하는 버전 및 길이 체크
        int maxLength;
        switch (version)
        {
            case 1: maxLength = 17; break;
            case 3: maxLength = 53; break;
            case 5: maxLength = 106; break;
            case 9: maxLength = 230; break;
            default: return new CommandResult(false, "Unsupported Version");
        }

        if (buf.Length > maxLength)
            return new CommandResult(false, "Data Length Exceeded");

        // 커맨드 데이터 조립
        byte mode = 0x02;
        byte dataLength = (byte)(buf.Length & 0xFF);
        byte type = (byte)version;
        byte[] cmd = new byte[] { 0x1A, 0x42, mode, dataLength, type };

        // 전송할 데이터 결합
        byte[] packet = new byte[cmd.Length + buf.Length];
        Buffer.BlockCopy(cmd, 0, packet, 0, cmd.Length);
        Buffer.BlockCopy(buf, 0, packet, cmd.Length, buf.Length);

        try
        {
            await transport.WriteAsync(packet);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    /// <summary>
    /// 바코드 프린트
    /// </summary>
    /// <param name="data">바코드에 넣을 데이터 (1 < data.length < 256)</param>
    /// <param name="size">바코드 가로 사이즈(3 ~ 9)</param>
    /// <returns>성공: 0, 실패: -1</returns>
    public async Task<CommandResult> barCode(string data, int size)
    {
        var transport = RequireTransport();

        if (string.IsNullOrEmpty(data))
            return new CommandResult(false, "Invalid Data");

        // 인코딩 바이트 길이로 체크 (특수문자 포함 대비)
        byte[] buf = System.Text.Encoding.GetEncoding("ks_c_5601-1987").GetBytes(data);

        // 커맨드 데이터 조립
        byte mode = 0x01;    // PDF417
        byte dataLength = (byte)(buf.Length & 0xFF);
        byte type = (byte)(size & 0xFF);
        byte[] cmd = new byte[] { 0x1A, 0x42, mode, dataLength, (byte)type, };

        // 전송할 데이터 결합
        byte[] packet = new byte[cmd.Length + buf.Length];
        Buffer.BlockCopy(cmd, 0, packet, 0, cmd.Length);
        Buffer.BlockCopy(buf, 0, packet, cmd.Length, buf.Length);

        try
        {
            await transport.WriteAsync(packet);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }

    public async Task<CommandResult> cmd(byte[] bytes)
    {
        var transport = RequireTransport();

        try
        {
            await transport.WriteAsync(bytes);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }
    #endregion
}
