using KIOSK.Infrastructure.OCR.Models;
using Pr22;
using Pr22.Imaging;
using Pr22.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.OCR.Providers
{
    public sealed class MrzOcrProvider : IOcrProvider
    {
        private readonly DocumentReaderDevice _dev;
        public MrzOcrProvider(DocumentReaderDevice dev) => _dev = dev;

        public async Task<OcrOutcome> RunAsync(Page page, CancellationToken ct)
        {
            try
            {
                var task = new Pr22.Task.EngineTask();
                task.Add(FieldSource.Mrz, FieldId.All);

                // PR22 엔진 호출은 Sync API -> Task.Run으로 스레드풀 위임
                var analyze = await Task.Run(() => _dev.Engine.Analyze(page, task), ct);

                var fields = analyze.GetFields();
                if (fields.Count == 0)
                    return new OcrOutcome { Success = false, Source = "MRZ", Error = "No MRZ fields detected." };

                // 신뢰도 필요하면: analyze.GetField(...).BestScore() 등 활용 (라이브러리 제공범위에 맞춰 보완)
                var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    //["Nationality"] = analyze.GetField(FieldSource.Mrz, FieldId.Nationality)?.GetBestStringValue(),
                    //["Name"] = analyze.GetField(FieldSource.Mrz, FieldId.Name)?.GetBestStringValue(),
                    //["DocumentNo"] = analyze.GetField(FieldSource.Mrz, FieldId.DocumentNumber)?.GetBestStringValue(),
                    ["Sex"] = analyze.GetField(FieldSource.Mrz, FieldId.Sex)?.GetBestStringValue(),
                    ["BirthDate"] = analyze.GetField(FieldSource.Mrz, FieldId.BirthDate)?.GetBestStringValue(),
                    ["GivenName"] = analyze.GetField(FieldSource.Mrz, FieldId.Givenname)?.GetBestStringValue(),
                    ["Surname"] = analyze.GetField(FieldSource.Mrz, FieldId.Surname)?.GetBestStringValue(),
                    ["ExpiryDate"] = analyze.GetField(FieldSource.Mrz, FieldId.ExpiryDate)?.GetBestStringValue(),

                    ["NO"] = analyze.GetField(FieldSource.Mrz, FieldId.DocumentNumber)?.GetBestStringValue(),
                    ["NAME"] = analyze.GetField(FieldSource.Mrz, FieldId.Name)?.GetBestStringValue(),
                    ["NATIONALITY"] = analyze.GetField(FieldSource.Mrz, FieldId.Nationality)?.GetBestStringValue(),

                }!.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value!);

                return new OcrOutcome
                {
                    Success = true,
                    Source = "MRZ",
                    DocumentType = "1",
                    Fields = result
                };
            }
            catch (Exception ex)
            {
                return new OcrOutcome { Success = false, Source = "MRZ", Error = ex.Message };
            }
        }
    }
}
