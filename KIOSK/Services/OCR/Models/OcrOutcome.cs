using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Services.OCR.Models
{
    public sealed class OcrOutcome
    {
        public bool Success { get; init; }
        public string? DocumentType { get; init; }  // 예: "KOR_ID", "Passport", ...
        public Dictionary<string, string>? Fields { get; init; } // 외부 OCR key-value
        public string? RawTypeJson { get; init; }
        public string? RawResultJson { get; init; }
        public string Source { get; init; } = "";   // "MRZ" or "External"
        public string? Error { get; init; }
    }
}
