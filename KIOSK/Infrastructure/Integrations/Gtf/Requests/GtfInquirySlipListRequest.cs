using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Requests;

public sealed class GtfInquirySlipListRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }

    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("passport_no")]
    public string? PassportNo { get; set; }

    [JsonPropertyName("nationality_code")]
    public string? NationalityCode { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("passport_expirdate")]
    public string? PassportExpirdate { get; set; }

    [JsonPropertyName("gender_code")]
    public string? GenderCode { get; set; }

    [JsonPropertyName("input_way_code")]
    public string? InputWayCode { get; set; } = "02";
}
