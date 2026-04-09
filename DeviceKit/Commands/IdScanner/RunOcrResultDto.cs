namespace DeviceKit.Commands.IdScanner;

/// <summary>
/// RUNOCR 명령 결과 DTO.
/// OCR 결과의 핵심 필드와 처리 상태를 전달한다.
/// </summary>
public sealed record RunOcrResultDto(
    bool Success,
    string Source,
    string? DocumentType,
    Dictionary<string, string> Fields,
    string? Error = null);
