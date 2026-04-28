using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Responses;

public sealed class GtfInitialResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }

    [JsonPropertyName("rm")]
    public string? Rm { get; set; }

    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }

    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }

    [JsonPropertyName("refund_limit_amt")]
    public string? RefundLimitAmt { get; set; }
}
