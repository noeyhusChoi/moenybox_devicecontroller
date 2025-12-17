// Abstractions/IDevice.cs
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Abstractions
{
    /// <summary>도메인 레벨 장치 인터페이스(명령/상태)</summary>
    public interface IDevice
    {
        string Name { get; }
        string Model { get; }

        Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default);

        // TODO: 스냅샷 ID를 사용한 상태 조회 삭제 및 처리 필요
        Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string snapshotId = "");

        /// <summary>제어 명령(장치별로 구체형 제공)</summary>
        Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default);
    }
}
