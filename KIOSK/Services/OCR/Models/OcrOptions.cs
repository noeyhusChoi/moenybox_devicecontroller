using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Services.OCR.Models
{
    public sealed class OcrOptions
    {
        // TODO: 기본 경로 변경 필수!
        public string BaseDir { get; init; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OCR");
        public string InputDir => Path.Combine(BaseDir, "input");
        public string ResultTypeDir => Path.Combine(BaseDir, "resultType");
        public string ResultDir => Path.Combine(BaseDir, "result");

        // 대기/타임아웃
        public TimeSpan ResultTimeout { get; init; } = TimeSpan.FromSeconds(10);
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(200);
    }
}
