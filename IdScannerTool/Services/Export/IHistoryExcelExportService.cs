namespace IdScannerTool.Services;

public interface IHistoryExcelExportService
{
    Task<string> ExportAsync(
        IReadOnlyList<HistoryExcelRow> rows,
        string filePath,
        CancellationToken cancellationToken = default);
}

public sealed record HistoryExcelRow(
    DateTimeOffset TimestampUtc,
    string DocumentNo,
    string Name,
    string Nationality);
