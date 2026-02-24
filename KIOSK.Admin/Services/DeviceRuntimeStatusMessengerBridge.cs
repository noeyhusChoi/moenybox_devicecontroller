using CommunityToolkit.Mvvm.Messaging;
using KIOSK.Admin.Messages;
using KIOSK.DeviceCommon.Devices;

namespace KIOSK.Admin.Services;

public sealed class DeviceRuntimeStatusMessengerBridge : IDisposable
{
    private readonly IDeviceRuntimeStatusEvents? _statusEvents;
    private readonly IMessenger _messenger;
    private bool _disposed;

    public DeviceRuntimeStatusMessengerBridge(IDeviceRuntimePort runtimePort, IMessenger messenger)
    {
        _messenger = messenger;
        _statusEvents = runtimePort as IDeviceRuntimeStatusEvents;
        if (_statusEvents is not null)
            _statusEvents.StatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(DeviceStatusSnapshot snapshot)
        => _messenger.Send(new DeviceStatusChangedMessage(snapshot));

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_statusEvents is not null)
            _statusEvents.StatusChanged -= OnStatusChanged;
    }
}

