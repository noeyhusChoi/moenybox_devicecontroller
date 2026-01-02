using KIOSK.Device.Abstractions;
using Pr22;
using Pr22.Events;
using Pr22.Imaging;
using Pr22.Processing;
using Pr22.Task;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;

namespace KIOSK.Device.Drivers;

public sealed class DeviceIdScanner : DeviceBase
{
    private DocumentReaderDevice? _device;
    private Pr22.Util.PresenceState _presenceState = Pr22.Util.PresenceState.Empty;
    private Page? _page;
    private readonly object _presenceLock = new();
    private bool _presenceSubscribed;
    private int _failThreshold;

    public event EventHandler<(int page, Light light, string path)>? ImageSaved;
    public event EventHandler<ScanEvent>? ScanSequence;

    public enum ScanEvent
    {
        Empty,
        Scanning,
        ScanComplete,
        Removed,
        RemovalTimeout
    }

    public DeviceIdScanner(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

            await DisposeReaderAsync().ConfigureAwait(false);

            var dev = new DocumentReaderDevice();
            var list = DocumentReaderDevice.GetDeviceList();
            if (list.Count == 0)
                throw new Pr22.Exceptions.NoSuchDevice("No device found.");

            dev.UseDevice(list[0]);
            _device = dev;

            Trace.WriteLine("Device connected: " + _device.DeviceName);
            _failThreshold = 0;

            return CreateSnapshot();
        }
        catch (Exception)
        {
            _failThreshold++;
            return CreateSnapshot(new[]
            {
                CreateAlarm("IDSCANNER", "미연결")
            });
        }
    }

    public override async Task<DeviceStatusSnapshot> GetStatusAsync(
        CancellationToken ct = default,
        string snapshotId = "")
    {
        var alarms = new List<DeviceAlarm>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var device = RequireDevice();
            var info = device.Scanner.Info;
            info.IsCalibrated();
            _failThreshold = 0;
        }
        catch (Exception)
        {
            _failThreshold++;
        }

        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("IDSCANNER", "응답 없음", Severity.Warning));

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            switch (command)
            {
                case { Name: string name } when name.Equals("SCANSTART", StringComparison.OrdinalIgnoreCase):
                    {
                        var res = await ScanStartAsync().ConfigureAwait(false);
                        return new CommandResult(res, "");
                    }

                case { Name: string name } when name.Equals("SCANSTOP", StringComparison.OrdinalIgnoreCase):
                    {
                        var res = await ScanStopAsync().ConfigureAwait(false);
                        return new CommandResult(res, "");
                    }

                case { Name: string name } when name.Equals("GETSCANSTATUS", StringComparison.OrdinalIgnoreCase):
                    {
                        if (!_presenceSubscribed)
                        {
                            await ScanStartAsync().ConfigureAwait(false);
                            return new CommandResult(false, "", null);
                        }

                        return new CommandResult(true, "", _presenceState);
                    }

                case { Name: string name } when name.Equals("SAVEIMAGE", StringComparison.OrdinalIgnoreCase):

                    {
                        var (res, page) = await SaveImageAsync().ConfigureAwait(false);
                        return new CommandResult(res, "", page);
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

    private Task<bool> ScanStartAsync()
    {
        try
        {
            var device = RequireDevice();

            lock (_presenceLock)
            {
                if (!_presenceSubscribed)
                {
                    device.PresenceStateChanged += OnPresence;
                    _presenceSubscribed = true;
                }
            }

            device.Scanner.StartTask(FreerunTask.Detection());
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<bool> ScanStopAsync()
    {
        try
        {
            var device = RequireDevice();

            lock (_presenceLock)
            {
                if (_presenceSubscribed)
                {
                    device.PresenceStateChanged -= OnPresence;
                    _presenceSubscribed = false;
                }
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<(bool, Page?)> SaveImageAsync()
    {
        try
        {
            var device = RequireDevice();
            var task = new DocScannerTask();
            task.Add(Light.White).Add(Light.Infra);

            var page = device.Scanner.Scan(task, PagePosition.First);
            _page = page;

            var saveDir = Path.Combine(Environment.CurrentDirectory, "ScanOutput");
            Directory.CreateDirectory(saveDir);

            try
            {
                var img = page.Select(Light.White).GetImage();
                var whitePath = Path.Combine(saveDir, $"scan_{Light.White}.jpg");
                img.Save(RawImage.FileFormat.Jpeg).Save(whitePath);
                ImageSaved?.Invoke(this, (1, Light.White, whitePath));

                img = page.Select(Light.Infra).GetImage();
                var infraPath = Path.Combine(saveDir, $"scan_{Light.Infra}.jpg");
                img.Save(RawImage.FileFormat.Jpeg).Save(infraPath);
                ImageSaved?.Invoke(this, (1, Light.Infra, infraPath));

                return Task.FromResult((true, _page));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"이미지 저장 실패 {ex.Message}");
                return Task.FromResult((false, (Page?)null));
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("Scan/Save 처리중 예외: " + ex);
            return Task.FromResult((false, (Page?)null));
        }
    }

    private void OnPresence(object? sender, DetectionEventArgs e)
    {
        try
        {
            _presenceState = e.State switch
            {
                Pr22.Util.PresenceState.Empty => Pr22.Util.PresenceState.Empty,
                Pr22.Util.PresenceState.Moving => Pr22.Util.PresenceState.Moving,
                Pr22.Util.PresenceState.Present => Pr22.Util.PresenceState.NoMove,
                Pr22.Util.PresenceState.NoMove => Pr22.Util.PresenceState.NoMove,
                _ => _presenceState
            };
        }
        catch (Exception ex)
        {
            Trace.WriteLine("OnPresence 처리 예외: " + ex);
        }
    }

    private DocumentReaderDevice RequireDevice()
        => _device ?? throw new InvalidOperationException("PR22 기기가 초기화되지 않았습니다.");

    private Task DisposeReaderAsync()
    {
        if (_device is null)
            return Task.CompletedTask;

        try { _device.Close(); } catch { }
        try { _device.Dispose(); } catch { }
        _device = null;

        return Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeReaderAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
