using ClosedXML.Excel;
using System.IO;

namespace IdScannerTool.Services;

public sealed class HistoryExcelExportService : IHistoryExcelExportService
{
    private static readonly string[] Headers =
    {
        "환전일자*",
        "환전고객명*",
        "환전고객_실명구분_코드*",
        "환전고객_실명번호*",
        "환전고객_국적부호*",
        "환전구분_코드*",
        "통화구분_코드*",
        "통화종류_코드*",
        "거래금액*",
        "매입률*",
        "원화금액*",
        "확인담당자",
        "제출은행_지점_코드*",
        "환전교부_증명서번호"
    };

    public Task<string> ExportAsync(
        IReadOnlyList<HistoryExcelRow> rows,
        string filePath,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("History");

            for (var i = 0; i < Headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = Headers[i];
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ws.Cell(rowIndex, 1).Value = row.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd");
                ws.Cell(rowIndex, 2).Value = row.Name;
                ws.Cell(rowIndex, 3).Value = string.Empty;
                ws.Cell(rowIndex, 4).Value = row.DocumentNo;
                ws.Cell(rowIndex, 5).Value = row.Nationality;
                ws.Cell(rowIndex, 6).Value = string.Empty;
                ws.Cell(rowIndex, 7).Value = string.Empty;
                ws.Cell(rowIndex, 8).Value = string.Empty;
                ws.Cell(rowIndex, 9).Value = string.Empty;
                ws.Cell(rowIndex, 10).Value = string.Empty;
                ws.Cell(rowIndex, 11).Value = string.Empty;
                ws.Cell(rowIndex, 12).Value = string.Empty;
                ws.Cell(rowIndex, 13).Value = string.Empty;
                ws.Cell(rowIndex, 14).Value = string.Empty;
                rowIndex++;
            }

            var headerRange = ws.Range(1, 1, 1, Headers.Length);
            headerRange.Style.Font.Bold = true;
            ws.Columns(1, Headers.Length).AdjustToContents();

            workbook.SaveAs(filePath);
            return filePath;
        }, cancellationToken);
}
