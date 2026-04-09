using System.Text.Json;

namespace DeviceKit.Events;

public static class DeviceEventJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(object? payload)
    {
        try
        {
            return JsonSerializer.Serialize(payload, Options);
        }
        catch
        {
            return "{}";
        }
    }

    public static T? Deserialize<T>(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, Options);
        }
        catch
        {
            return default;
        }
    }
}
