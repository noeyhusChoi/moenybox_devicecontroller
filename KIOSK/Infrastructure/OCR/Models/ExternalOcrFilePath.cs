using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.OCR.Models
{
    public sealed class ExternalOcrFilePath
    {
        public string SessionId { get; init; } = "0001"; // 거래번호, 반드시 4자리
        public string InfraImagePath { get; init; } = default!;     // 분석 이미지 (infra)
        public string WhiteImagePath { get; init; } = default!;     // 분석 이미지 (white)
        public string TriggerPath { get; init; } = default!;        // 분석 시작 트리거 파일
        public string TypeJsonPath { get; init; } = default!;       // 신분증 타입 분석 결과 Json
        public string ResultJsonPath { get; init; } = default!;     // 신분증 내용 분석 결과 Json
    }
}
