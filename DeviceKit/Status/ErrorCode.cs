namespace DeviceKit.Status;

public readonly record struct ErrorCode(string Domain, string Device, string Category, string Detail)
{
    public override string ToString() => $"{Domain}.{Device}.{Category}.{Detail}";

    public static bool TryParse(string code, out ErrorCode result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return false;

        result = new ErrorCode(parts[0], parts[1], parts[2], parts[3]);
        return true;
    }
}
