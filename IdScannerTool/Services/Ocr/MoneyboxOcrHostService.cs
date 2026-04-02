using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace IdScannerTool.Services;

public sealed record ExternalOcrProcessOptions(
    string ExecutablePath);

/// <summary>
/// 외부 OCR 엔진(moneybox_ocr.exe) 프로세스를 앱 라이프사이클에 맞춰 관리한다.
/// </summary>
public sealed class MoneyboxOcrHostService : IHostedService, IDisposable
{
    private readonly ExternalOcrProcessOptions _options;
    private readonly ILogger<MoneyboxOcrHostService> _logger;
    private readonly object _sync = new();
    private Process? _process;
    private bool _ownsProcess;

    public MoneyboxOcrHostService(
        ExternalOcrProcessOptions options,
        ILogger<MoneyboxOcrHostService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_process is not null && !_process.HasExited)
            {
                return Task.CompletedTask;
            }

            if (!File.Exists(_options.ExecutablePath))
            {
                throw new FileNotFoundException(
                    $"External OCR executable not found: {_options.ExecutablePath}",
                    _options.ExecutablePath);
            }

            var executableFullPath = Path.GetFullPath(_options.ExecutablePath);
            var existing = FindRunningProcess(executableFullPath);
            if (existing is not null)
            {
                _process = existing;
                _ownsProcess = true;
                _logger.LogInformation(
                    "External OCR process already running. Reusing and adopting existing process. Path={Path}, PID={Pid}",
                    executableFullPath,
                    existing.Id);
                return Task.CompletedTask;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executableFullPath,
                WorkingDirectory = Path.GetDirectoryName(executableFullPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            process.Exited += OnProcessExited;

            if (!process.Start())
            {
                process.Dispose();
                throw new InvalidOperationException($"Failed to start external OCR executable: {executableFullPath}");
            }

            _process = process;
            _ownsProcess = true;
            _logger.LogInformation("External OCR process started. Path={Path}, PID={Pid}", executableFullPath, process.Id);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopProcess();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopProcess();
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (sender is Process process)
        {
            _logger.LogWarning("External OCR process exited. ExitCode={ExitCode}", process.ExitCode);
        }
    }

    private void StopProcess()
    {
        Process? process;
        bool ownsProcess;

        lock (_sync)
        {
            process = _process;
            ownsProcess = _ownsProcess;
            _process = null;
            _ownsProcess = false;
        }

        if (process is null)
        {
            return;
        }

        try
        {
            process.Exited -= OnProcessExited;

            if (!process.HasExited)
            {
                var closed = false;
                try
                {
                    closed = process.CloseMainWindow();
                }
                catch
                {
                    // no-op: console/non-window process
                }

                if (closed)
                {
                    if (!process.WaitForExit(2000))
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(3000);
                    }
                }
                else
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }

            _logger.LogInformation("External OCR process stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop external OCR process cleanly.");
        }
        finally
        {
            process.Dispose();
        }
    }

    private static Process? FindRunningProcess(string executableFullPath)
    {
        var processName = Path.GetFileNameWithoutExtension(executableFullPath);
        var candidates = Process.GetProcessesByName(processName);

        foreach (var candidate in candidates)
        {
            try
            {
                if (candidate.HasExited)
                {
                    continue;
                }

                var currentPath = TryGetExecutablePath(candidate);
                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    continue;
                }

                if (string.Equals(
                        Path.GetFullPath(currentPath),
                        executableFullPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
            catch
            {
                // Access denied / process race: ignore candidate
            }
        }

        return null;
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            var modulePath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(modulePath))
            {
                return modulePath;
            }
        }
        catch
        {
            // Fallback below
        }

        nint handle = 0;
        try
        {
            handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);
            if (handle == 0)
            {
                return null;
            }

            var buffer = new StringBuilder(1024);
            var size = buffer.Capacity;
            if (!QueryFullProcessImageName(handle, 0, buffer, ref size))
            {
                return null;
            }

            return buffer.ToString(0, size);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (handle != 0)
            {
                _ = CloseHandle(handle);
            }
        }
    }

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(nint hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint hObject);
}
