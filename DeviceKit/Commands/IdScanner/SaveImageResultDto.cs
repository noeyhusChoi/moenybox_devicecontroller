namespace DeviceKit.Commands.IdScanner;

/// <summary>
/// SAVEIMAGE 명령 결과 DTO.
/// Page 원본 객체 대신 직렬화 가능한 형태(이미지 경로/바이트)를 제공한다.
/// </summary>
public sealed record SaveImageResultDto(
    string ImagePath,
    byte[] ImageByte,
    string? WhiteImagePath = null);
