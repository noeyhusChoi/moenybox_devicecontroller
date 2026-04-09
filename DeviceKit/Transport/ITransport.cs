// Abstractions/ITransport.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Transport
{
    /// <summary>
    /// 바이트 스트림 전송 계층(시리얼, TCP, 가상포트 등 교체 가능)
    /// </summary>
    internal interface ITransport : IAsyncDisposable
    {
        event EventHandler? Disconnected;
        bool IsOpen { get; }
        Task OpenAsync(CancellationToken ct = default);
        Task CloseAsync(CancellationToken ct = default);
        Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default);
        Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default);
    }
}
