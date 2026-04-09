using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kiosk.Infrastructure.Database.Repositories;

public sealed class BufferedDeviceCommandLogSink : IDeviceCommandLogSink, IHostedService
{
    private readonly DeviceCommandLogRepository _inner;
    private readonly ILogger<BufferedDeviceCommandLogSink> _logger;
    private readonly DeviceCommandLogOptions _options;
    private readonly Channel<DeviceCommandRecord> _channel;
    private readonly bool _isBufferedMode;
    private readonly CancellationTokenSource _workerCts = new();
    private Task? _workerTask;

    public BufferedDeviceCommandLogSink(
        DeviceCommandLogRepository inner,
        IOptions<DeviceCommandLogOptions> options,
        ILogger<BufferedDeviceCommandLogSink> logger)
    {
        _inner = inner;
        _logger = logger;
        _options = options.Value;
        _isBufferedMode =
            _options.Enabled &&
            string.Equals(_options.Mode, "Buffered", StringComparison.OrdinalIgnoreCase);

        var capacity = Math.Max(1, _options.QueueCapacity);
        var fullMode = _options.DropWhenFull
            ? BoundedChannelFullMode.DropWrite
            : BoundedChannelFullMode.Wait;

        _channel = Channel.CreateBounded<DeviceCommandRecord>(new BoundedChannelOptions(capacity)
        {
            FullMode = fullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isBufferedMode)
            return Task.CompletedTask;

        _workerTask = Task.Run(() => RunAsync(_workerCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isBufferedMode)
            return;

        _channel.Writer.TryComplete();

        if (_workerTask is null)
            return;

        var timeoutMs = Math.Max(100, _options.StopFlushTimeoutMs);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            await _workerTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _workerCts.Cancel();
            _logger.LogWarning("Buffered device command log sink stop timed out. pending logs may be dropped.");
        }
    }

    public Task WriteAsync(DeviceCommandRecord record, CancellationToken ct = default)
    {
        if (!_isBufferedMode)
            return _inner.WriteAsync(record, ct);

        if (_channel.Writer.TryWrite(record))
            return Task.CompletedTask;

        if (_options.DropWhenFull)
            return Task.CompletedTask;

        return _channel.Writer.WriteAsync(record, ct).AsTask();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var record in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await _inner.WriteAsync(record, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist device command log record.");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }
}

