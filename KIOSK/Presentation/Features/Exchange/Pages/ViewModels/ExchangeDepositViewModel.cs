using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Domain.Entities;
using KIOSK.Application.Services;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Infrastructure.Common.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeDepositViewModel : ObservableObject, IStepMain, IStepNext, IStepError, INavigable
    {
        #region Trigger
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }
        #endregion

        #region RightSection
        [ObservableProperty]
        private string videoPath;

        [ObservableProperty]
        private ObservableCollection<ExchangeDepositNoteAsset> currencyNotes;   // 화폐 참고 이미지
        #endregion

        #region LeftSection
        [ObservableProperty]
        private Uri selectedCurrencyFlag;   // 선택 화폐 플래그

        [ObservableProperty]
        private decimal dailyLimitAmount;   // 1일 최대 한도

        [ObservableProperty]
        private decimal dailyReaminAmount = 1000;  // 1일 잔여 한도

        [ObservableProperty]
        private decimal perLimitAmount;     // 1회 최대 한도
        #endregion

        // 입금 여부 ( 버튼 활성화 )
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanNext))]
        [NotifyPropertyChangedFor(nameof(CanExit))]
        private bool isDeposit = false;         // 입금 여부

        public bool CanNext => IsDeposit;       // 거래 종료 버튼 활성화 여부
        public bool CanExit => !IsDeposit;      // 다음 버튼 활성화 여부

        private readonly ITransactionContext _transactionService;                // 거래 기록 서비스
        private readonly IExchangeDepositUseCase _depositUseCase;
        private readonly IExchangeDepositAssetsProvider _depositAssetsProvider;
        // 바인딩 데이터
        public TransactionModelV2 Transaction => _transactionService.Current;

        public ExchangeDepositViewModel(
            ITransactionContext transactionService,
            IExchangeDepositUseCase depositUseCase,
            IExchangeDepositAssetsProvider depositAssetsProvider)
        {
            // TODO: 사용 가능 화폐 단위 모델 참조 형식으로 변경 필요 ( 시스템 설정에서 사용 가능 화폐 단위 )
            // TODO: 현재 선택 화폐 참조 형식으로 변경 필요 ( 유저 선택 화폐 )
            // TODO: 이미지 추출 유틸리티로 추후 이동

            _transactionService = transactionService;
            _depositUseCase = depositUseCase;
            _depositAssetsProvider = depositAssetsProvider;
            _depositUseCase.DepositStateChanged += OnDepositStateChanged;
            CurrencyNotes = new ObservableCollection<ExchangeDepositNoteAsset>();

            // 선택 화폐 국가 이미지
            SelectedCurrencyFlag = new Uri($"pack://application:,,,/Assets/FLAG/{Transaction.SourceCurrency}.png", UriKind.Absolute);
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            var assets = await _depositAssetsProvider.LoadAsync(Transaction.SourceCurrency, ct);
            VideoPath = assets.VideoPath;
            CurrencyNotes = new ObservableCollection<ExchangeDepositNoteAsset>(assets.CurrencyNotes);

            await _depositUseCase.StartAsync(ct);
        }

        public async Task OnUnloadAsync()
        {
            await _depositUseCase.StopAsync(CancellationToken.None);
            _depositUseCase.DepositStateChanged -= OnDepositStateChanged;
        }

        private void OnDepositStateChanged(bool hasDeposit)
        {
            IsDeposit = hasDeposit;
        }

        #region Commands
        [RelayCommand]
        private async Task Main()
        {
            try
            {
                await _depositUseCase.StopAsync(CancellationToken.None);
                if (OnStepMain is not null) 
                    await OnStepMain();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        [RelayCommand]
        private async Task Previous()
        {
            try
            {
                await _depositUseCase.StopAsync(CancellationToken.None);
                if (OnStepPrevious is not null)
                    await OnStepPrevious();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        [RelayCommand]
        private async Task Next()
        {
            try
            {
                await _depositUseCase.StopAsync(CancellationToken.None);
                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion

    }
}