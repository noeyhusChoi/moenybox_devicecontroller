
namespace DeviceKit.Commands;

public sealed record DeviceCommandResponse(
    bool Success,
    string Message = "",
    object? Data = null,
    ErrorCode? Code = null);
