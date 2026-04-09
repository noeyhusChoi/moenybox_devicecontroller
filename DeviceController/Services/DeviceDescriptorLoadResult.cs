namespace DeviceController.Services;

public sealed record DeviceDescriptorLoadResult(
    IReadOnlyList<DeviceDescriptor> Descriptors,
    string SourceLabel,
    string Summary,
    string Diagnostics,
    bool LoadedFromDatabase);
