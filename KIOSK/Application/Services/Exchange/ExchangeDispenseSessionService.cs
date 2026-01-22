using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Application.Services.Transactions;
using KIOSK.Infrastructure.Common.Utils;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeDispenseSessionService : IExchangeDispenseSessionService
    {
        private readonly ITransactionServiceV2 _transactionService;
        private readonly WithdrawalCassetteService _withdrawalCassetteService;
        private readonly IWithdrawalDeviceService _withdrawalDeviceService;

        public ExchangeDispenseSessionService(
            ITransactionServiceV2 transactionService,
            WithdrawalCassetteService withdrawalCassetteService,
            IWithdrawalDeviceService withdrawalDeviceService)
        {
            _transactionService = transactionService;
            _withdrawalCassetteService = withdrawalCassetteService;
            _withdrawalDeviceService = withdrawalDeviceService;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            await _withdrawalCassetteService.InitializeAsync();
            var cassettes = _withdrawalCassetteService.Get();

            await _transactionService.PlanPayoutsAsync(cassettes.ToList());

            var packets = _transactionService.BuildDevicePackets(use20K: false);

            foreach (var (deviceId, payload) in packets)
            {
                var resultMap = await _withdrawalDeviceService.DispenseAsync(deviceId, payload, ct);
                if (resultMap == null)
                    continue;

                _transactionService.ApplyDeviceResults(deviceId, resultMap);
            }

            var json = JsonConvertExtension.ConvertToJson(_transactionService.Current);
            Trace.WriteLine(json);
            await _withdrawalCassetteService.ResultAsync(json, default);
        }
    }
}
