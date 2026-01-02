using System;
using System.Collections.Generic;

namespace KIOSK.Device.Abstractions;

/// <summary>
/// Vendor+Model 기반으로 드라이버 키를 해석한다.
/// Driver 필드는 향후 확장을 위해 유지하지만 현재는 사용하지 않는다.
/// </summary>
public static class DeviceDriverResolver
{
    private static readonly Dictionary<(string Vendor, string Model), string> Map =
        new(new VendorModelComparer())
        {
            { ("TOTINFO", "E200Z"), "QR_TOTINFO" },
            { ("NEWLAND", "EM20"), "QR_NEWLAND" },
            { ("GENERIC", "PRINTER"), "PRINTER" },
            { ("PR22", "IDSCANNER"), "IDSCANNER" },
            { ("HCDM", "10K"), "HCDM10K" },
            { ("HCDM", "20K"), "HCDM20K" },
            { ("MPOS", "DEPOSIT"), "DEPOSIT" }
        };

    public static string Resolve(DeviceDescriptor descriptor)
    {
        var vendor = (descriptor.Vendor ?? string.Empty).Trim();
        var model = (descriptor.Model ?? string.Empty).Trim();

        if (Map.TryGetValue((vendor, model), out var driver))
            return driver;

        // 호환성 유지: Vendor가 없으면 모델을 그대로 드라이버 키로 사용
        if (string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(model))
            return model;

        return model;
    }

    private sealed class VendorModelComparer : IEqualityComparer<(string Vendor, string Model)>
    {
        public bool Equals((string Vendor, string Model) x, (string Vendor, string Model) y)
            => string.Equals(x.Vendor, y.Vendor, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Model, y.Model, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Vendor, string Model) obj)
            => HashCode.Combine(
                obj.Vendor?.ToUpperInvariant(),
                obj.Model?.ToUpperInvariant());
    }
}
