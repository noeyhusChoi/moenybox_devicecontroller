using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Application.Services.Localization;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Presentation.Shared.Abstractions;
using Localization;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfLanguageSelectViewModel : StepViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<LocaleInfoModel> localeField;

        private readonly ILocalizationService _localizationService;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;
        private readonly ILocaleInfoProvider _localeInfoProvider;

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

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

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
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is string selectedLanguage)
            {
                try
                {
                    var culture = new CultureInfo(selectedLanguage);

                    _localizationService.SetCulture(culture);

                    await ExecuteStepAsync(OnStepNext, selectedLanguage);
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
