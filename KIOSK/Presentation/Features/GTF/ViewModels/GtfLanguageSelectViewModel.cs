using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Application.Services.Localization;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.ViewModels;
using Localization;
using System.Collections.ObjectModel;
using System.Globalization;
using KIOSK.Domain.Entities;

namespace KIOSK.Presentation.Features.GTF.ViewModels
{
    public partial class GtfLanguageSelectViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        [ObservableProperty]
        private ObservableCollection<LocaleInfoModel> localeField;

        private readonly ILocalizationService _localizationService;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;
        private readonly ILocaleInfoProvider _localeInfoProvider;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public GtfLanguageSelectViewModel(ILocalizationService localizationService, ILocaleInfoProvider localeInfoProvider, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _localizationService = localizationService;
            _localeInfoProvider = localeInfoProvider;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;

            _gtfTaxRefundService.Reset();   // 모델 초기화
            var usingLanguage = new[]
            {
                "ZH-CN", "ZH-TW", "EN-GB", 
                "JA-JP", "FR-FR", "ES-ES", 
                "TH-TH", "MS-MY", "ID-ID", 
                "RU-RU", "AR-SA", "KO-KR"
            };

            LocaleField = new ObservableCollection<LocaleInfoModel>(
                _localeInfoProvider.LocaleInfoList
                    .Where(f => usingLanguage.Contains(f.CultureCode))
                    .OrderBy(f => Array.IndexOf(usingLanguage, f.CultureCode))
            );
        }

        public Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        private async Task InitAsync(CancellationToken ct)
        {
            // TODO : 여기에서 삭제, 실행 시 체크, 실패 시 성공까지 주기적 전송,
            InitialRequestDto req = new InitialRequestDto()
            {
                Edi = "01",
                TmlId = "A1",
                ShopName = "테스트1"
            };

            var res = await _gtfApiService.InitialAsync(req, ct);
            
            _gtfTaxRefundService.ApplyInitialResponse(req, res);
        }

        #region Commands
        [RelayCommand]
        private async Task Main()
        {
            try
            {
                if (OnStepMain is not null)
                    await OnStepMain();
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }

        [RelayCommand]
        private async Task Previous()
        {
            try
            {
                if (OnStepPrevious is not null)
                    await OnStepPrevious();
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is string selectedLanguage)
            {
                try
                {
                    var culture = new CultureInfo(selectedLanguage);

                    _localizationService.SetCulture(culture);

                    if (OnStepNext is not null)
                        await OnStepNext(selectedLanguage);
                }
                catch (Exception ex)
                {
                    OnStepError?.Invoke(ex);
                }
            }
        }
        #endregion
    }
}
