// Abstractions/ITransport.cs
using System;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Abstractions
{
    /// <summary>
    /// 바이트 스트림 전송 계층(시리얼, TCP, 가상포트 등 교체 가능)
    /// </summary>
    public interface ITransport : IAsyncDisposable
    {
        event EventHandler? Disconnected;
        bool IsOpen { get; }
        Task OpenAsync(CancellationToken ct = default);
        Task CloseAsync(CancellationToken ct = default);
        Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default);
        Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default);
    }
}
