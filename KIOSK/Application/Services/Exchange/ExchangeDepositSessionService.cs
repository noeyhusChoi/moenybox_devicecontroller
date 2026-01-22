using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Application.Services.Transactions;
using KIOSK.Application.Abstractions;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeDepositSessionService : IExchangeDepositSessionService
    {
        private readonly IDepositDevice _depositDevice;
        private readonly ITransactionServiceV2 _transactionService;
        private readonly WithdrawalCassetteService _withdrawalCassetteService;
        private readonly ILoggingService _logging;
        private readonly IUiDispatcher _uiDispatcher;

        public ExchangeDepositSessionService(
            IDepositDevice depositDevice,
            ITransactionServiceV2 transactionService,
            WithdrawalCassetteService withdrawalCassetteService,
            ILoggingService logging,
            IUiDispatcher uiDispatcher)
        {
            _depositDevice = depositDevice;
            _transactionService = transactionService;
            _withdrawalCassetteService = withdrawalCassetteService;
            _logging = logging;
            _uiDispatcher = uiDispatcher;
        }

        public event Action<bool>? DepositStateChanged;

        public async Task StartAsync(CancellationToken ct)
        {
            _depositDevice.Escrowed += OnEscrowed;
            await _withdrawalCassetteService.InitializeAsync();
            await _depositDevice.StartAsync(ct);
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _depositDevice.Escrowed -= OnEscrowed;
            await _depositDevice.StopAsync(ct);
        }

        private void OnEscrowed(object? sender, string s)
        {
            _ = HandleEscrowedAsync(s);
        }

        private async Task HandleEscrowedAsync(string s)
        {
            try
            {
                var note = s.Split(' ');
                string currency = note[0];
                decimal denom = decimal.Parse(note[1]);

                var transaction = _transactionService.Current;

                if (transaction.TargetRequestedAmount is not null)
                {
                    if (!currency.Equals(transaction.SourceCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        await _depositDevice.ReturnAsync(CancellationToken.None);
                        return;
                    }

                    await _uiDispatcher.InvokeAsync(() =>
                    {
                        transaction.AddOrIncrement(currency, denom, +1);
                    });

                    await _depositDevice.StackAsync(CancellationToken.None);

                    if (transaction.TargetRequestedAmount <= transaction.TargetComputedAmount)
                    {
                        await _depositDevice.StopAsync(CancellationToken.None);
                    }
                }
                else
                {
                    if (!currency.Equals(transaction.SourceCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        await _depositDevice.ReturnAsync(CancellationToken.None);
                        return;
                    }

                    var cassetteAmount = _withdrawalCassetteService.GetTotalAmount(transaction.TargetCurrency);
                    var requiredAmount = transaction.TargetComputedAmount + (denom * transaction.CurrencyPair.Rate);

                    if (cassetteAmount < requiredAmount)
                    {
                        await _depositDevice.ReturnAsync(CancellationToken.None);
                        return;
                    }

                    await _uiDispatcher.InvokeAsync(() =>
                    {
                        transaction.AddOrIncrement(currency, denom, +1);
                    });

                    await _depositDevice.StackAsync(CancellationToken.None);
                }

                DepositStateChanged?.Invoke(transaction.Deposits.Count > 0);
            }
            catch (Exception ex)
            {
                _logging.Error(ex, ex.Message);
            }
        }
    }
}
