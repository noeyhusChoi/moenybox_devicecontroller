namespace DeviceKit.Drivers.HCDM20K;

internal enum Hcdm20kCommand : byte
{
    Sensor = (byte)'S',
    Initialize = (byte)'T',
    Version = (byte)'V',
    Eject = (byte)'J',
    Dispense = (byte)'D'
}
