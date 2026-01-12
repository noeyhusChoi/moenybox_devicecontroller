// Device/Abstractions/DeviceDescriptor.cs
namespace KIOSK.Device.Abstractions
{
    /// <summary>
    /// 하나의 시리얼(또는 TCP 등) 장치를 정의하는 메타데이터입니다.
    /// DeviceRegistry가 이 정보를 바탕으로 Transport/Protocol/Device 인스턴스를 조립합니다.
    /// </summary>
    /// <param name="Name">
    ///   장치의 고유 식별자(예: "scale-1", "prn-1"). DeviceManager.SendAsync시 해당 값 사용
    /// </param>
    /// <param name="Vendor">
    ///   제조사 식별자(예: "TOTINFO", "NEWLAND")
    /// </param>
    /// <param name="Model">
    ///   장치 모델 식별자(예: "E200Z", "EM20")
    /// </param>
    /// <param name="TransportType">
    ///   통신 타입
    ///   - 예: "TCP", "SERIAL"
    /// </param>
    /// <param name="TransportParam">
    ///   통신 파라미터
    ///   - TCP 예: "192.168.0.50:5000"
    ///   - SERIAL 예: "COM10@19200"
    /// </param>
    /// <param name="ProtocolName">
    ///   프로토콜/프레이머 선택 키(예: "AsciiLine", "SimpleCRC"). DeviceRegistry.CreateProtocol 분기
    /// </param>
    /// <param name="PollingMs">
    ///   상태 확인 주기(ms). DeviceManager의 GetStatusAsync 호출 간격
    /// </param>
    /// <param name="DeviceType">
    ///   장치 타입(예: "QR", "PRINTER"). 표준 코드 발행 시 사용
    /// </param>
    /// <param name="Driver">
    ///   드라이버 타입(예: "QR_E200Z", "WITHDRAWAL_HCDM10K")
    /// </param>
    public sealed record DeviceDescriptor(
        string Name,                    // PRINTER-1
        string Vendor,                  // TOTINFO
        string Model,                   // E200Z
        string TransportType,           // TCP
        string TransportPort,           // COM10 / 172.0.0.1
        string TransportParam,          // 19200,8,1,None / 54321
        string ProtocolName = "",       // ASCII, SimpleCRC 등 확장 가능
        int PollingMs = 10000,
        bool Validate = true,       // 유효성 검사 여부
        string DeviceType = "",      // 표준 코드용 장치 타입
        string Driver = ""           // 드라이버 타입
    );
}
