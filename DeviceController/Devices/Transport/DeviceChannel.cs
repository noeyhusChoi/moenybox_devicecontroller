using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Transport;

/// <summary>
/// ITransport 위에서 동기 요청/응답과 비동기 수신을 동시에 다룰 수 있는 경량 채널.
/// - 프레임 단위(IFramer)로 수신을 분리하고, SendAndWait로 특정 응답을 기다릴 수 있다.
/// </summary>
public sealed class DeviceChannel : IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly IFramer _framer;
    private readonly EventHandler _transportDisconnectedHandler;
    private readonly Pipe _pipe = new();
    private readonly List<PendingResponse> _pending = new();
    private readonly object _pendingLock = new();
    private CancellationTokenSource? _cts;
    private Task? _readerTask;
    private Task? _parserTask;
    private Exception? _stopReason;

    public event Action<ReadOnlyMemory<byte>>? FrameReceived;

    public DeviceChannel(ITransport transport, IFramer? framer = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _framer = framer ?? PassthroughFramer.Instance;

        // 트랜스포트 단에서 끊김을 감지하면 채널의 대기중 요청을 즉시 정리한다.
        _transportDisconnectedHandler = (_, __) => Stop(new IOException("Transport disconnected."));
        _transport.Disconnected += _transportDisconnectedHandler;
    }

    public bool IsRunning => _cts is { IsCancellationRequested: false };

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        _stopReason = null;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Task.Run에 token을 넘기면 "시작 전 취소"가 TaskCanceled로 바뀌며, 원인 추적이 어려워질 수 있다.
        _readerTask = Task.Run(() => ReadLoopAsync(_cts.Token));
        _parserTask = Task.Run(() => ParseLoopAsync(_cts.Token));

        return Task.CompletedTask;
    }

    /// <summary>응답을 기다리지 않고 프레임을 전송.</summary>
    public async Task WriteAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (!IsRunning)
            await StartAsync(ct).ConfigureAwait(false);

        if (_cts is null)
            throw new InvalidOperationException("Channel not started.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        var frame = _framer.MakeFrame(payload.Span);
        await _transport.WriteAsync(frame, linked.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 요청을 전송하고 지정된 matcher에 일치하는 프레임을 기다립니다.
    /// </summary>
    public async Task<byte[]> SendAndWaitAsync(
        ReadOnlyMemory<byte> payload,
        Func<ReadOnlyMemory<byte>, bool>? matcher = null,
        int timeoutMs = 1000,
        CancellationToken ct = default)
    {
        if (!IsRunning)
            await StartAsync(ct).ConfigureAwait(false);

        if (_cts is null)
            throw new InvalidOperationException("Channel not started.");

        var waiter = new PendingResponse(matcher ?? (_ => true));
        lock (_pendingLock)
        {
            _pending.Add(waiter);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
        timeoutCts.CancelAfter(timeoutMs);
        using var reg = timeoutCts.Token.Register(() => waiter.Tcs.TrySetException(new TimeoutException($"WaitAsync timed out after {timeoutMs}ms.")));

        try
        {
            var frame = _framer.MakeFrame(payload.Span);
            await _transport.WriteAsync(frame, linked.Token).ConfigureAwait(false);
            return await waiter.Tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (_pendingLock)
            {
                _pending.Remove(waiter);
            }
        }
    }

    /// <summary>
    /// 전송 없이, 지정된 matcher에 일치하는 다음 프레임을 기다립니다.
    /// </summary>
    public async Task<byte[]> WaitAsync(
        Func<ReadOnlyMemory<byte>, bool>? matcher = null,
        int timeoutMs = 1000,
        CancellationToken ct = default)
    {
        if (!IsRunning)
            await StartAsync(ct).ConfigureAwait(false);

        if (_cts is null)
            throw new InvalidOperationException("Channel not started.");

        var waiter = new PendingResponse(matcher ?? (_ => true));
        lock (_pendingLock)
        {
            _pending.Add(waiter);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
        timeoutCts.CancelAfter(timeoutMs);
        using var reg = timeoutCts.Token.Register(() => waiter.Tcs.TrySetCanceled(timeoutCts.Token));

        try
        {
            return await waiter.Tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (_pendingLock)
            {
                _pending.Remove(waiter);
            }
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            if (!_transport.IsOpen)
                await _transport.OpenAsync(ct).ConfigureAwait(false);

            while (!ct.IsCancellationRequested)
            {
                Memory<byte> mem = _pipe.Writer.GetMemory(2048);
                int read = await _transport.ReadAsync(mem, ct).ConfigureAwait(false);
                if (read <= 0)
                    continue;

                _pipe.Writer.Advance(read);
                var flush = await _pipe.Writer.FlushAsync(ct).ConfigureAwait(false);
                if (flush.IsCompleted)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            Stop(new IOException("Read loop failed."));
        }
        finally
        {
            _pipe.Writer.Complete();
        }
    }

    private async Task ParseLoopAsync(CancellationToken ct)
    {
        try
        {
            while (true)
            {
                var result = await _pipe.Reader.ReadAsync(ct).ConfigureAwait(false);
                var buffer = result.Buffer;

                while (_framer.TryExtractFrame(ref buffer, out var frame))
                {
                    DispatchFrame(frame.ToArray());
                }

                _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _pipe.Reader.Complete();
        }
    }

    private void DispatchFrame(byte[] frame)
    {
        PendingResponse? matched = null;

        lock (_pendingLock)
        {
            matched = _pending.FirstOrDefault(p => p.Matcher(frame));
            if (matched != null)
                _pending.Remove(matched);
        }

        if (matched != null)
        {
            matched.Tcs.TrySetResult(frame);
            return;
        }

        try
        {
            FrameReceived?.Invoke(frame);
        }
        catch
        {
            // 외부 구독자 예외로 파서 루프가 죽지 않도록 보호
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { _transport.Disconnected -= _transportDisconnectedHandler; } catch { }

        Stop(new OperationCanceledException("Channel disposed."));

        try
        {
            if (_readerTask != null)
                await _readerTask.ConfigureAwait(false);
        }
        catch { }

        try
        {
            if (_parserTask != null)
                await _parserTask.ConfigureAwait(false);
        }
        catch { }

        _pipe.Reader.Complete();
        _pipe.Writer.Complete();
    }

    private void Stop(Exception reason)
    {
        _stopReason ??= reason;

        try { _cts?.Cancel(); } catch { }

        PendingResponse[] pending;
        lock (_pendingLock)
        {
            pending = _pending.ToArray();
            _pending.Clear();
        }

        foreach (var p in pending)
        {
            if (reason is OperationCanceledException oce)
                p.Tcs.TrySetCanceled(oce.CancellationToken);
            else
                p.Tcs.TrySetException(reason);
        }
    }

    private sealed record PendingResponse(Func<ReadOnlyMemory<byte>, bool> Matcher)
    {
        public TaskCompletionSource<byte[]> Tcs { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>
/// 프레이밍을 사용하지 않는 장비용 기본 프레이머.
/// </summary>
public sealed class PassthroughFramer : IFramer
{
    public static readonly PassthroughFramer Instance = new();
    private PassthroughFramer() { }

    public bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        if (buffer.Length == 0)
        {
            frame = default;
            return false;
        }

        frame = buffer;
        buffer = buffer.Slice(buffer.End);
        return true;
    }

    public byte[] MakeFrame(ReadOnlySpan<byte> payload)
        => payload.ToArray();
}
