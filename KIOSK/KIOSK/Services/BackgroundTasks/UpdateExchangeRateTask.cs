using KIOSK.Infrastructure.Logging;
using KIOSK.Models;
using KIOSK.Services.API;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIOSK.Services.BackgroundTasks
{
    /// <summary>
    /// 환율 정보 주기적 갱신 작업.
    /// </summary>
    public sealed class UpdateExchangeRateTask
    {
        private readonly CemsApiService _cems;
        private readonly ExchangeRateModel _model;
        private readonly ILoggingService _logger;

        public UpdateExchangeRateTask(CemsApiService cems, ExchangeRateModel model, ILoggingService logger)
        {
            _cems = cems;
            _model = model;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            var result = await _cems.GetRateAllAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            if (!result.Fields.TryGetValue("data", out var dataJson) || string.IsNullOrWhiteSpace(dataJson))
            {
                _model.Result = false;
                _model.Data = new ObservableCollection<ExchangeRate>();
            }
            else
            {
                var list = JsonSerializer.Deserialize<ObservableCollection<ExchangeRate>>(dataJson, options)
                           ?? new ObservableCollection<ExchangeRate>();

                _model.Result = result.Result;
                _model.Data = list;
            }

            if (_model.Result && _model.Data != null)
            {
                const decimal scale = 0.01m;
                foreach (var data in _model.Data)
                {
                    switch (data.Currency)
                    {
                        case "VND":
                        case "JPY":
                        case "IDR":
                            data.Base *= scale;
                            data.Sell *= scale;
                            data.Buy *= scale;
                            data.SpSell *= scale;
                            data.SpBuy *= scale;
                            break;
                    }
                }
            }
        }
    }
}
