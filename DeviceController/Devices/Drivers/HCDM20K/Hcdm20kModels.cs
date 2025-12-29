namespace KIOSK.Devices.Drivers.HCDM20K;

public enum Hcdm20kCommand : byte
{
    Sensor = (byte)'S',
    Initialize = (byte)'T',
    Version = (byte)'V',
    Eject = (byte)'J',
    Dispense = (byte)'D'
}
