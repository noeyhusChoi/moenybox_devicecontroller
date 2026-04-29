using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Responses;

public sealed class GtfInquirySlipListResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }

    [JsonPropertyName("rm")]
    public string? Rm { get; set; }

    [JsonPropertyName("passport_serial_no")]
    public string? PassportSerialNo { get; set; }
}
