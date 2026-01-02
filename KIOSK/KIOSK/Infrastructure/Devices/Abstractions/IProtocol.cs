// Abstractions/IProtocol.cs
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Abstractions
{
    /// <summary>
    /// 장치별: 요청 payload -> 응답 payload 매핑/파싱(프레이밍+인코딩 포함)
    /// </summary>
    public interface IProtocol
    {
        Task<byte[]> ExchangeAsync(ITransport transport, byte[] requestPayload,
            int responseTimeoutMs = 500, CancellationToken ct = default);
    }
}
