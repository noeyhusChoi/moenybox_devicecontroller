using Kiosk.Application.Services.Devices.IdScanner;
using Microsoft.Extensions.Logging;

namespace Kiosk.Application.Features.ExchangeV2.Services;

public interface IExchangeScanSession
{
    event EventHandler<IdScannerEvent>? ProgressChanged;

    Task<ExchangeScanSessionResult> ExecuteAsync(TimeSpan timeout, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}

public sealed record ExchangeScanSessionResult(
    bool Success,
    ScanCaptureResult? Capture = null,
    ScanOcrResult? Ocr = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed class ExchangeScanSession : IExchangeScanSession
{
    private readonly IIdScannerService _idScannerService;
    private readonly ILogger<ExchangeScanSession> _logger;
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private bool _running;

    public ExchangeScanSession(
        IIdScannerService idScannerService,
        ILogger<ExchangeScanSession> logger)
    {
        _idScannerService = idScannerService;
        _logger = logger;
    }

    public event EventHandler<IdScannerEvent>? ProgressChanged;

    public async Task<ExchangeScanSessionResult> ExecuteAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_running)
            {
                return new ExchangeScanSessionResult(
                    false,
                    ErrorCode: "SYS.EXCHANGE.SCAN.ALREADY_RUNNING",
                    ErrorMessage: "Exchange scan session is already running.");
            }

            _running = true;
        }
        finally
        {
            _runGate.Release();
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        using var registration = timeoutCts.Token.Register(() => completion.TrySetCanceled(timeoutCts.Token));

        void OnScannerEvent(object? sender, IdScannerEvent e)
        {
            try
            {
                ProgressChanged?.Invoke(this, e);

                if (e is IdScanStatusChangedEvent status)
                {
                    if (status.Phase == IdScannerScanPhase.ScanComplete)
                        completion.TrySetResult(true);

                    if (status.Phase == IdScannerScanPhase.Faulted)
                        completion.TrySetException(new InvalidOperationException("Scanner entered a faulted state."));
                }

                if (e is IdScannerFaultedEvent fault)
                    completion.TrySetException(new InvalidOperationException($"{fault.Code}: {fault.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unhandled scan session event processing error.");
            }
        }

        _idScannerService.EventReceived += OnScannerEvent;

        try
        {
            var start = await _idScannerService.StartScanAsync(ct).ConfigureAwait(false);
            if (!start.Success)
            {
                return new ExchangeScanSessionResult(
                    false,
                    ErrorCode: start.ErrorCode,
                    ErrorMessage: start.ErrorMessage);
            }

            await completion.Task.ConfigureAwait(false);

            var capture = await _idScannerService.SaveImageAsync(ct).ConfigureAwait(false);
            if (!capture.Success)
            {
                return new ExchangeScanSessionResult(
                    false,
                    Capture: capture,
                    ErrorCode: capture.ErrorCode,
                    ErrorMessage: capture.ErrorMessage);
            }

            var ocr = await _idScannerService.RunOcrAsync(capture, ct).ConfigureAwait(false);
            if (!ocr.Success)
            {
                return new ExchangeScanSessionResult(
                    false,
                    Capture: capture,
                    Ocr: ocr,
                    ErrorCode: ocr.ErrorCode,
                    ErrorMessage: ocr.ErrorMessage);
            }

            return new ExchangeScanSessionResult(true, capture, ocr);
        }
        catch (OperationCanceledException)
        {
            return new ExchangeScanSessionResult(
                false,
                ErrorCode: "SYS.EXCHANGE.SCAN.TIMEOUT",
                ErrorMessage: "Exchange scan session timed out or was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exchange scan session failed.");
            return new ExchangeScanSessionResult(
                false,
                ErrorCode: "SYS.EXCHANGE.SCAN.FAILED",
                ErrorMessage: ex.Message);
        }
        finally
        {
            _idScannerService.EventReceived -= OnScannerEvent;
            try
            {
                await _idScannerService.StopScanAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to stop scanner at the end of exchange scan session.");
            }

            await _runGate.WaitAsync().ConfigureAwait(false);
            try
            {
                _running = false;
            }
            finally
            {
                _runGate.Release();
            }
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        await _idScannerService.StopScanAsync(ct).ConfigureAwait(false);

        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _running = false;
        }
        finally
        {
            _runGate.Release();
        }
    }
}
