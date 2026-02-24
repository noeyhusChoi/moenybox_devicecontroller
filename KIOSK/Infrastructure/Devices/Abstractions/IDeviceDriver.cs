// Abstractions/IDevice.cs
namespace KIOSK.Device.Abstractions
{
    /// <summary>인프라 레벨 드라이버 인터페이스(명령/상태)</summary>
    public interface IDeviceDriver
    {
        string Name { get; }
        string Model { get; }

        Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default);

        // TODO: 스냅샷 ID를 사용한 상태 조회 삭제 및 처리 필요
        Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default);

        /// <summary>제어 명령(장치별로 구체형 제공)</summary>
        Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default);
    }
}
