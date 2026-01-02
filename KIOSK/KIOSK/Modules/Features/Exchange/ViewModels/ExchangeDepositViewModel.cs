using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Device.Drivers;
using KIOSK.Devices.Management;
using KIOSK.Models;
using KIOSK.Services;
using KIOSK.Utils;
using MPOST;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KIOSK.Infrastructure.Media;

namespace KIOSK.ViewModels
{
    // 화폐 이미지
    public class CurrencyNoteItem
    {
        public int Denomination { get; set; }
        public string Label => Denomination.ToString();
        public ImageSource Image { get; set; }
        public string FilePath { get; set; } // 필요시
    }

    public partial class ExchangeDepositViewModel : ObservableObject, IStepMain, IStepNext, IStepError, INavigable
    {
        #region Trigger
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }
        #endregion

        #region RightSection
        private Uri videoPath;    // Gif 경로
        
        [ObservableProperty]
        private Brush backgroundBrush;

        [ObservableProperty]
        private ObservableCollection<CurrencyNoteItem> currencyNotes;   // 화폐 참고 이미지
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

        // 에셋 폴더
        private readonly string _assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Image", "Denomination");

        // 화폐 이미지 최대 로드 개수
        private readonly int _maxCount = 7;

        // 입금 여부 ( 버튼 활성화 )
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanNext))]
        [NotifyPropertyChangedFor(nameof(CanExit))]
        private bool isDeposit = false;         // 입금 여부

        public bool CanNext => IsDeposit;       // 거래 종료 버튼 활성화 여부
        public bool CanExit => !IsDeposit;      // 다음 버튼 활성화 여부

        private readonly IDeviceManager _deviceManager;                            // 장비 관리자
        private readonly ITransactionServiceV2 _transactionService;                // 거래 기록 서비스
        private readonly WithdrawalCassetteService _withdrawalCassetteService;     // 시재 관리 서비스
        private readonly IVideoPlayService _videoPlay;

        // 바인딩 데이터
        public TransactionModelV2 Transaction => _transactionService.Current;

        public ExchangeDepositViewModel(
            IDeviceManager deviceManager, 
            ITransactionServiceV2 transactionService, 
            WithdrawalCassetteService withdrawalCassetteService, 
            IVideoPlayService videoPlay)
        {
            // TODO: 사용 가능 화폐 단위 모델 참조 형식으로 변경 필요 ( 시스템 설정에서 사용 가능 화폐 단위 )
            // TODO: 현재 선택 화폐 참조 형식으로 변경 필요 ( 유저 선택 화폐 )
            // TODO: 이미지 추출 유틸리티로 추후 이동

            _deviceManager = deviceManager;
            _transactionService = transactionService;
            _withdrawalCassetteService = withdrawalCassetteService;
            _videoPlay = videoPlay;

            // 선택 화폐 국가 이미지
            SelectedCurrencyFlag = new Uri($"pack://application:,,,/Assets/FLAG/{Transaction.SourceCurrency}.png", UriKind.Absolute);

            // 선택 화폐 입금 방법 GIF 영상
            try
            {
                // TODO: 파일 존재 유무 체크
                videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
            }
            catch (IOException ex)
            {
                // 파일을 찾지 못했을 때
                //_logging?.Error(ex, ex.Message);
            }
            catch (Exception ex)
            {
                // 그 외 예외
                //_logging?.Error(ex, ex.Message);
            }

            BackgroundBrush = _videoPlay.BackgroundBrush;
            _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);

        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // 선택 화폐 이미지 로드
            await LoadDenominationAsync(Transaction.SourceCurrency);

            // 카세트 정보 로드
            await _withdrawalCassetteService.InitializeAsync();

            // 입금기 시작
            await OnStartDepositAsync();
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        private async void HandleEscrowed(object? sender, string s)
        {
            // 입금 화폐 정보 파싱
            var note = s.Split(' ');
            string currency = note[0];
            decimal denom = decimal.Parse(note[1]);

            // 외화 구매 일 때,
            // 원화 입금 / 외화 방출 / 거스름돈 방출
            // Source = KRW, Target = USD
            if (Transaction.TargetRequestedAmount is not null)
            {
                // 2차버전 외화 구매 미지원.
                return;
                // 거래 화폐 불일치 체크
                if (!currency.Equals(Transaction.SourceCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Return"));
                    Trace.WriteLine($"입금 화폐 불일치: 선택 {Transaction.SourceCurrency} / 입금 {currency}");
                    return;
                }

                // TODO: 허용 가능 어트리뷰트

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Transaction.AddOrIncrement(currency, denom, +1);
                }), DispatcherPriority.Background);


                Trace.WriteLine($"타겟: {Transaction.TargetCurrency}");
                Trace.WriteLine($"입금: {Transaction.SourceDepositedTotal}{Transaction.SourceCurrency}");
                Trace.WriteLine($"출금: {Transaction.TargetComputedAmount}{Transaction.TargetCurrency}");


                _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Stack"));

                // 목표 금액 완료 시 입금 정지
                if (Transaction.TargetRequestedAmount <= Transaction.TargetComputedAmount)
                {
                    _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Stop"));
                }
            }
            // 외화 입금 / 원화 방출 
            // Source = USD, Target = KRW
            else
            {
                // 거래 화폐 불일치 체크
                if (!currency.Equals(Transaction.SourceCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Return"));
                    Trace.WriteLine($"입금 화폐 불일치: 선택 {Transaction.SourceCurrency} / 입금 {currency}");
                    return;
                }

                // TODO: 허용 가능 어트리뷰트

                // 시재 한도 체크
                var cassetteAmount = _withdrawalCassetteService.GetTotalAmount(Transaction.TargetCurrency);
                var requiredAmount = Transaction.TargetComputedAmount + (denom * Transaction.CurrencyPair.Rate);

                if (cassetteAmount < requiredAmount)
                {
                    _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Return"));
                    Trace.WriteLine($"환전 한도 초과: {cassetteAmount} / {requiredAmount}");
                    return;
                }


                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Transaction.AddOrIncrement(currency, denom, +1);
                }), DispatcherPriority.Background);

                Trace.WriteLine($"타겟: {Transaction.TargetCurrency}");
                Trace.WriteLine($"입금: {Transaction.SourceDepositedTotal}{Transaction.SourceCurrency}");
                Trace.WriteLine($"출금: {Transaction.TargetComputedAmount}{Transaction.TargetCurrency}");


                _ = _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Stack"));
            }

            if (Transaction.Deposits.Count > 0)
                IsDeposit = true;
        }

        private async Task OnStartDepositAsync()
        {
            var deposit = _deviceManager.GetDevice<DeviceDeposit>("DEPOSIT1");
            if (deposit != null)
            {
                deposit.OnEscrowed += HandleEscrowed;
            }

            var x = await _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Start"));
        }

        private async Task OnStopDepositAsync()
        {
            var deposit = _deviceManager.GetDevice<DeviceDeposit>("DEPOSIT1");
            if (deposit != null)
            {
                deposit.OnEscrowed -= HandleEscrowed;
            }
            var x = await _deviceManager.SendAsync("DEPOSIT1", new DeviceCommand("Stop"));
        }

        private async Task LoadDenominationAsync(string currencyCode)
        {
            string[] _supportedExt = new[] { ".png", ".jpg" };

            if (CurrencyNotes != null)
                CurrencyNotes.Clear();
            else
                CurrencyNotes = new();

            if (string.IsNullOrWhiteSpace(currencyCode) || !Directory.Exists(_assetsDir)) return;

            var files = Directory.GetFiles(_assetsDir)
                .Where(f => _supportedExt.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Where(f => Path.GetFileName(f).StartsWith(currencyCode + "_", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var list = files.Select(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var m = Regex.Match(name, @"^.+[_\-](\d+)$");
                int denom = 0;
                if (m.Success) int.TryParse(m.Groups[1].Value, out denom);
                return new { File = f, Denom = denom };
            })
            .OrderBy(x => x.Denom == 0 ? int.MaxValue : x.Denom)
            .ThenBy(x => x.File)
            .Take(_maxCount)
            .ToArray();

            // 비동기 로드: UI 스레드가 아닌 곳에서 수행
            var tasks = list.Select(async item =>
            {
                // 캐시 확인 — 캐시에 없으면 파일 스트림으로 안전히 만들기
                var bmp = ImageCacheExtension.GetOrAdd(item.File, () =>
                {
                    try
                    {
                        using var fs = new FileStream(item.File, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // 필요시
                        bi.DecodePixelWidth = 240;
                        bi.StreamSource = fs;
                        bi.EndInit();
                        bi.Freeze(); // cross-thread safe
                        return bi;
                    }
                    catch
                    {
                        return null;
                    }
                });

                return new CurrencyNoteItem
                {
                    Denomination = item.Denom,
                    Image = bmp,
                    FilePath = item.File
                };
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            // UI 스레드에서 컬렉션에 추가
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var it in results)
                {
                    CurrencyNotes.Add(it);
                }
            });
        }

        #region Commands
        [RelayCommand]
        private async Task Main()
        {
            try
            {
                await OnStopDepositAsync();
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
                await OnStopDepositAsync();
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
                await OnStopDepositAsync();
                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion

        #region DebugButton
        // Debug에서만 활성화 (Code & UI)
        [RelayCommand]
        private void Increase(object param)
        {
#if DEBUG
            HandleEscrowed(null, $"{Transaction.SourceCurrency} {param}");
#endif
        }

        [RelayCommand]
        private void Decrease(object param)
        {
#if DEBUG
          
#endif
        }
        #endregion
    }
}
