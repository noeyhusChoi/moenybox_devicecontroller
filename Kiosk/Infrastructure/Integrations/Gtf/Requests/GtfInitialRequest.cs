using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Requests;

public sealed class GtfInitialRequest
{
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }

    [JsonPropertyName("tml_id")]
    public string? TmlId { get; set; }

    [JsonPropertyName("shop_name")]
    public string? ShopName { get; set; }
}
