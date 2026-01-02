using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace KIOSK.Models;

public class ExchangeRateModel
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }

    [JsonPropertyName("data")]
    public ObservableCollection<ExchangeRate> Data { get; set; } = new();
}

public class ExchangeRate
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("base")]
    public decimal? Base { get; set; }

    [JsonPropertyName("buy")]
    public decimal? Buy { get; set; }

    [JsonPropertyName("sell")]
    public decimal? Sell { get; set; }

    [JsonPropertyName("spbuy")]
    public decimal? SpBuy { get; set; }

    [JsonPropertyName("spsell")]
    public decimal? SpSell { get; set; }

    [JsonIgnore]
    public Uri FlagUri => new Uri($"pack://application:,,,/Assets/FLAG/{Currency.ToUpperInvariant()}.png", UriKind.Absolute);
}