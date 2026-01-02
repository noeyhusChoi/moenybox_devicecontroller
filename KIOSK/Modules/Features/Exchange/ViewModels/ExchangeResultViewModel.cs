using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Models;
using KIOSK.Services;
using KIOSK.Infrastructure.Database;
using KIOSK.Services.API;
using Localization;
using MySqlConnector;
using System.Data;
using System.Diagnostics;
using System.Transactions;

namespace KIOSK.ViewModels
{
    public partial class ExchangeResultViewModel : ObservableObject, IStepMain, IStepNext, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        [ObservableProperty]
        private string testCurrency = "USD";

        [ObservableProperty]
        private decimal testDeposit = 123456;

        [ObservableProperty]
        private decimal test = 12;


        [ObservableProperty]
        private string selectedCurrency;    // 선택 화폐

        [ObservableProperty]
        private decimal selectedExchangeRate;   // 선택 화폐 환율

        [ObservableProperty]
        private Uri selectedCurrencyFlag;   // 선택 화폐 플래그

        [ObservableProperty]
        private decimal depositAmount;  // 입금 금액

        [ObservableProperty]
        private decimal withdrawalAmount;  // 출금 금액

        // 상세내용
        [ObservableProperty] private decimal withrawalAmount50000;
        [ObservableProperty] private decimal withrawalAmount10000;
        [ObservableProperty] private decimal withrawalAmount5000;
        [ObservableProperty] private decimal withrawalAmount1000;

        private readonly ILocalizationService _localizationService;
        private readonly ITransactionServiceV2 _transactionService;
        private readonly ReceiptPrintService _receiptPrintService;
        private readonly CemsApiService _cemsApiService;
        private readonly IDatabaseService _databaseService;
        public TransactionModelV2 Transaction => _transactionService.Current;

        public ExchangeResultViewModel(ITransactionServiceV2 transactionService, ILocalizationService localizationService, IDatabaseService databaseService, ReceiptPrintService receiptPrintService, CemsApiService cemsApiService)
        {
            _localizationService = localizationService;
            _transactionService = transactionService;
            _receiptPrintService = receiptPrintService;
            _cemsApiService = cemsApiService;
            _databaseService = databaseService;

            SelectedCurrency = Transaction.CurrencyPair.BaseCurrency;
            SelectedExchangeRate = Transaction.CurrencyPair.Rate;
            SelectedCurrencyFlag = new Uri($"pack://application:,,,/Assets/FLAG/{Transaction.CurrencyPair.BaseCurrency}.png", UriKind.Absolute);

            DepositAmount = Transaction.SourceDepositedTotal;       // 입금 금액
            WithdrawalAmount = Transaction.TargetComputedAmount;    // 환전 금액

            // 상세내용

            // 출금 성공 금액
            WithrawalAmount50000 = Transaction.TargetPayouts
                                              .Where(x => x.Denomination == 50_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                                              .Sum(x => x.SucceededCount);
            WithrawalAmount10000 = Transaction.TargetPayouts
                                              .Where(x => x.Denomination == 10_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                                              .Sum(x => x.SucceededCount);
            WithrawalAmount5000 = Transaction.TargetPayouts
                                              .Where(x => x.Denomination == 5_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                                              .Sum(x => x.SucceededCount);
            WithrawalAmount1000 = Transaction.TargetPayouts
                                              .Where(x => x.Denomination == 1_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                                              .Sum(x => x.SucceededCount);

            Trace.WriteLine($"{WithrawalAmount50000} {WithrawalAmount10000} {WithrawalAmount5000} {WithrawalAmount1000}");
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            var res = await _cemsApiService.RegisterTransactionAsync(Transaction, ct);
            if (res.Result && res.ECode == null)
            {
                await _databaseService.QueryAsync<DataTable>(@"sp_update_tx_outbox_success",
                new[]
                {
                                DatabaseService.Param("@tx_id", MySqlDbType.VarChar, Transaction.TransactionID)
                },
                type: CommandType.StoredProcedure);
            }
            else
            {
                await _databaseService.QueryAsync<DataTable>(@"sp_update_tx_outbox_fail",
                new[]
                {
                                DatabaseService.Param("@tx_id", MySqlDbType.VarChar, Transaction.TransactionID)
                },
                type: CommandType.StoredProcedure);
            }
            Trace.WriteLine(res.Result);
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        #region Commands
        [RelayCommand]
        private async Task Next(object? parameter)
        {
            try
            {
                if (parameter is bool b && b)
                {
                    var cultureName = _localizationService.CurrentCulture.Name;

                    if (cultureName.StartsWith("ko"))
                    {
                        await _receiptPrintService.PrintReceiptAsync("ko-KR", Transaction);
                    }
                    else
                    {
                        await _receiptPrintService.PrintReceiptAsync("en-US", Transaction);
                    }
                }

                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
        #endregion
    }
}
