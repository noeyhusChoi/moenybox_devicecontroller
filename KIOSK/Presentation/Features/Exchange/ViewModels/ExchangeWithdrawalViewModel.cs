using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Application.Services;
using KIOSK.Infrastructure.Common.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using KIOSK.Infrastructure.Media;

namespace KIOSK.ViewModels
{
    public partial class ExchangeWithdrawalViewModel : ObservableObject, IStepNext, INavigable
    {
        //public Func<Task>? OnStepMain { get; set; }
        //public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private Uri videoPath;

        [ObservableProperty]
        private Brush backgroundBrush;

        private readonly IDeviceManager _deviceManager;
        private readonly ITransactionServiceV2 _transactionService;
        private readonly WithdrawalCassetteService _withdrawalCassetteService;
        private readonly IVideoPlayService _videoPlay;

        public ExchangeWithdrawalViewModel(IDeviceManager deviceManager, ITransactionServiceV2 transaction, WithdrawalCassetteService withdrawalCassetteService, IVideoPlayService videoPlay)
        {
            _deviceManager = deviceManager;
            _transactionService = transaction;
            _withdrawalCassetteService = withdrawalCassetteService;
            _videoPlay = videoPlay;

            _videoPlay = videoPlay;

            // TODO: 로딩 시 필요한 작업 수행
            try
            {
                // TODO: 파일 존재 유무 체크
                videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
            }
            catch (IOException)
            {
                // 파일을 찾지 못했을 때
            }
            catch (Exception)
            {
                // 그 외 예외
            }

            BackgroundBrush = _videoPlay.BackgroundBrush;
            _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _withdrawalCassetteService.InitializeAsync();
            var cassettes = _withdrawalCassetteService.Get();

            // 출금 계획/실행
            // 0) (필요 시) 계획 수립
            //TODO: HashSet, List 매개변수 차이
            await _transactionService.PlanPayoutsAsync(cassettes.ToList());

            // 1) 페이로드 빌드
            var packets = _transactionService.BuildDevicePackets(use20K: false);

            // 2) 외부에서 장치 전송
            var allResults = new Dictionary<string, Dictionary<int, (int req, int exit, int rej)>>();
            foreach (var (deviceId, payload) in packets)
            {
                //var map = await _deviceManager.SendAsync(deviceId, new DeviceCommand("Dispense", payload)); // 외부 구현

                // 2-1. 명령 전송
                var response = await _deviceManager.SendAsync(deviceId, new DeviceCommand("Dispense", payload));
                Trace.WriteLine("출금 명령 전송");

                if (response.Success)
                {
                    if (response.Data is byte[] rawBytes)
                    {
                        // 2-2. 응답 파싱 (ASCII → 구조)
                        var resultMap = ParseDispenseResponse(rawBytes);

                        // 2-3. 장치별 결과 저장 (allResults 용)
                        allResults[deviceId] = resultMap;

                        // 3) 결과 반영
                        _transactionService.ApplyDeviceResults(deviceId, resultMap);

                        //foreach(var x in resultMap)
                        //{
                        //    Trace.WriteLine($"결과 반영 {deviceId} {x.Key} {x.Value.req} {x.Value.exit} {x.Value.rej}");
                        //}
                    }
                }
                else
                {
                    Trace.WriteLine($"Dispense Command Failed for Device {deviceId}, {response.Message}");
                }
            }

            //// 4) 재고 차감 목록 생성 → 외부 DB 서비스로 전달
            //var decrements = _transactionService.GetDecrementChanges();
            //await _withdrawalCassetteService.WithdrawalAsync(decrements, default); // 외부 구현

            // 5) 거래 결과 및 시재 차감→ 외부 DB 서비스로 전달
            var json = JsonConvertExtension.ConvertToJson(_transactionService.Current);
            Trace.WriteLine(json);
            await _withdrawalCassetteService.ResultAsync(json, default); // 외부 구현

            // 6) 다음 페이지
            await Next(true);
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        public static Dictionary<int, (int req, int exit, int rej)> ParseDispenseResponse20K(byte[] data)
        {
            // --- helpers ---
            int Digit(byte b) => (b >= 0x30 && b <= 0x39) ? b - 0x30 : -1;

            int ReadBlock(byte[] data, int n)
            {
                int count = data.Length / 3;     // 3바이트 = 1개 정수
                if (count > n) count = n;        // 최대 n개까지만

                int baseIndex = n * 3;

                // ASCII '0'(0x30)~'9'(0x39) 기준 변환
                int hundreds = data[baseIndex] - 0x30;
                int tens = data[baseIndex + 1] - 0x30;
                int ones = data[baseIndex + 2] - 0x30;

                int result = hundreds * 100 + tens * 10 + ones;

                return result;
            }

            //if (data == null || data.Length == 0) return map;

            int n = Digit(data[0]);                // N
            //if (n <= 0) return map;

            int groupCount = Digit(data[0]);

            ReadOnlySpan<byte> payload = data[1..];

            if (groupCount <= 0)
                return null;

            if (payload.Length % groupCount != 0)
            {
                Trace.WriteLine("Dispense Response Failed");
                return null;
            }

            int chunkSize = payload.Length / groupCount;

            var chunks = new List<byte[]>(groupCount);
            int offset = 0;

            for (int i = 0; i < groupCount; i++)
            {
                var slice = payload.Slice(offset, chunkSize);
                chunks.Add(slice.ToArray());
                offset += chunkSize;
            }

            var map = new Dictionary<int, (int, int, int)>();

            foreach (var chunk in chunks)
            {
                var req = ReadBlock(chunk, 0);
                var exit = ReadBlock(chunk, 2);
                var rej = ReadBlock(chunk, 3);

                map.Add(map.Count, (req, exit, rej));
            }

            return map;
        }

        public static Dictionary<int, (int req, int exit, int rej)> ParseDispenseResponse(byte[] data)
        {
            // --- helpers ---
            int Digit(byte b) => (b >= 0x30 && b <= 0x39) ? b - 0x30 : -1;

            int ReadBlock(byte[] data, int n)
            {
                int count = data.Length / 3;     // 3바이트 = 1개 정수
                if (count > n) count = n;        // 최대 n개까지만

                int baseIndex = n * 3;

                // ASCII '0'(0x30)~'9'(0x39) 기준 변환
                int hundreds = data[baseIndex] - 0x30;
                int tens = data[baseIndex + 1] - 0x30;
                int ones = data[baseIndex + 2] - 0x30;

                int result = hundreds * 100 + tens * 10 + ones;

                return result;
            }

            ReadOnlySpan<byte> payload = data[5..];

            const int chunkSize = 13;

            if (payload.Length % chunkSize != 0)
            {
                Trace.WriteLine("Dispense Response Failed");
                return null;
            }

            int groupCount = payload.Length / chunkSize;
            var chunks = new List<byte[]>(groupCount);

            int offset = 0;
            for (int i = 0; i < groupCount; i++)
            {
                var slice = payload.Slice(offset, chunkSize);
                chunks.Add(slice.ToArray());
                offset += chunkSize;
            }

            var map = new Dictionary<int, (int, int, int)>();

            foreach (var chunk in chunks)
            {
                var req = chunk[0];
                var exit = chunk[4];
                var rej = chunk[5];

                map.Add(map.Count, (req, exit, rej));
            }

            return map;
        }


        #region Commands
        [RelayCommand]
        private async Task Next(object? o)
        {
            try
            {
                OnStepNext?.Invoke("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
