using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeIdScanGuideUseCase : IExchangeIdScanGuideUseCase
    {
        private readonly IIdScannerDevice _scanner;

        public ExchangeIdScanGuideUseCase(IIdScannerDevice scanner)
        {
            _scanner = scanner;
        }

        public async Task<bool> ScanUntilStableAsync(CancellationToken ct)
        {
            int stableCount = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var startRes = await _scanner.ScanStartAsync(ct);
                if (!startRes.Success)
                {
                    await Task.Delay(150, ct);
                    continue;
                }

                var status = await _scanner.GetScanStatusAsync(ct);
                var presence = status.Data is Pr22.Util.PresenceState state
                    ? state
                    : (Pr22.Util.PresenceState?)null;

                switch (presence)
                {
                    case Pr22.Util.PresenceState.Empty:
                    case Pr22.Util.PresenceState.Dirty:
                    case Pr22.Util.PresenceState.Moving:
                        stableCount = 0;
                        break;
                    case Pr22.Util.PresenceState.Present:
                    case Pr22.Util.PresenceState.NoMove:
                        if (++stableCount >= 5)
                            return status.Success;
                        break;
                }

                await Task.Delay(200, ct);
            }
        }

        public async Task StopAsync(CancellationToken ct)
        {
            try
            {
                await _scanner.ScanStopAsync(ct);
            }
            catch
            {
            }
        }
    }
}
