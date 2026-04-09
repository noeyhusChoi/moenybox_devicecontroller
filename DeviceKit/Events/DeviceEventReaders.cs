using DeviceKit.Events.Payloads;

namespace DeviceKit.Events;

public static class DeviceEventReaders
{
    public static QrDecodedPayload? TryReadQrDecoded(DeviceEventEnvelope envelope)
    {
        if (!string.Equals(envelope.EventName, DeviceEventNames.QrDecoded, StringComparison.OrdinalIgnoreCase))
            return null;

        return DeviceEventJson.Deserialize<QrDecodedPayload>(envelope.PayloadJson);
    }

    public static DepositEscrowedPayload? TryReadDepositEscrowed(DeviceEventEnvelope envelope)
    {
        if (!string.Equals(envelope.EventName, DeviceEventNames.DepositEscrowed, StringComparison.OrdinalIgnoreCase))
            return null;

        return DeviceEventJson.Deserialize<DepositEscrowedPayload>(envelope.PayloadJson);
    }

    public static IdScannerDocumentDetectedPayload? TryReadIdScannerDocumentDetected(DeviceEventEnvelope envelope)
    {
        if (!string.Equals(envelope.EventName, DeviceEventNames.IdScannerDocumentDetected, StringComparison.OrdinalIgnoreCase))
            return null;

        return DeviceEventJson.Deserialize<IdScannerDocumentDetectedPayload>(envelope.PayloadJson);
    }

    public static IdScannerScanStatusChangedPayload? TryReadIdScannerScanStatusChanged(DeviceEventEnvelope envelope)
    {
        if (!string.Equals(envelope.EventName, DeviceEventNames.IdScannerScanStatusChanged, StringComparison.OrdinalIgnoreCase))
            return null;

        return DeviceEventJson.Deserialize<IdScannerScanStatusChangedPayload>(envelope.PayloadJson);
    }

    public static IdScannerImageSavedPayload? TryReadIdScannerImageSaved(DeviceEventEnvelope envelope)
    {
        if (!string.Equals(envelope.EventName, DeviceEventNames.IdScannerImageSaved, StringComparison.OrdinalIgnoreCase))
            return null;

        return DeviceEventJson.Deserialize<IdScannerImageSavedPayload>(envelope.PayloadJson);
    }
}
