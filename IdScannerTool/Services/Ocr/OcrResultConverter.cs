using System.Globalization;

namespace IdScannerTool.Services;

/// <summary>
/// OCR 결과 -> 저장 규칙 정규화
/// </summary>
public sealed class OcrResultConverter : IOcrResultConverter
{
    // 앱 내부 문서 타입 기준: 01=주민등록증/운전면허증, 02=여권, 03=외국인등록증
    private static readonly HashSet<string> ResidentDocumentTypes = new(StringComparer.Ordinal)
    {
        "01",
        "03"
    };

    // 3자리(ISO-3) -> 2자리(ISO-2) 국가코드 변환 맵.
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Alpha3ToAlpha2Map = new(CreateCountryCodeMap);

    public RunOcrResultDto Normalize(RunOcrResultDto source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!source.Success)
        {
            return source;
        }

        // 원본 DTO는 유지하고, 필드 사본에 후처리
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source.Fields is { Count: > 0 })
        {
            foreach (var pair in source.Fields)
            {
                fields[pair.Key] = pair.Value ?? string.Empty;
            }
        }

        // 문서타입 정규화
        var documentType = ResolveDocumentType(source.Source, source.DocumentType, fields);
        if (!string.IsNullOrWhiteSpace(documentType))
        {
            fields["DOCUMENTTYPE"] = documentType;
        }

        // 한국 신분증
        // 실명번호/국적/생년월일 정규화 (한국 신분증, xxxxxx-1xxxxxx 형식)
        if (!string.IsNullOrWhiteSpace(documentType) && ResidentDocumentTypes.Contains(documentType))
        {
            ApplyResidentDocumentRules(fields);
        }

        // 국가 코드 정규화
        // 모든 케이스에서 국가코드를 최종 2자리 코드로 정규화.
        NormalizeNationality(fields);

        return source with
        {
            DocumentType = documentType,
            Fields = fields
        };
    }

    private static void ApplyResidentDocumentRules(IDictionary<string, string> fields)
    {
        // 오류 : 문서번호 필드 없음
        if (!fields.TryGetValue("NO", out var rawDocumentNo) || string.IsNullOrWhiteSpace(rawDocumentNo))
        {
            return;
        }

        // 1. 문서번호의 하이픈/공백 등 특수문자 제거.
        var normalizedDocumentNo = RemoveSpecialCharacters(rawDocumentNo);
        fields["NO"] = normalizedDocumentNo;


        // 오류 : 문서번호 13자리 미만 (yymmdd1xxxxxx 최소 13자리)
        if (normalizedDocumentNo.Length < 13)
        {
            return;
        }

        // 오류 : 7번째 자리존재 여부 (yymmdd1xxxxxx 형식)
        if (normalizedDocumentNo.Length < 7)
        {
            return;
        }
        
        var seventh = normalizedDocumentNo[6];
        
        // 2. 국적
        // 7번째 자리(0-based index 6) => 1~4 : KR, 5~8 : 외국인
        if (seventh is >= '1' and <= '4')
        {
            fields["NATIONALITY"] = "KR";
        }


        // 3. 생년월일
        // 앞 6자리(yymmdd)를 생년월일(yyyy-MM-dd)로 반영.
        var birthYymmdd = normalizedDocumentNo[..6];
        if (TryConvertBirthDate(birthYymmdd, seventh, out var birthDate))
        {
            fields["BIRTHDATE"] = birthDate;
        }

    }

    private static void NormalizeNationality(IDictionary<string, string> fields)
    {
        if (!fields.TryGetValue("NATIONALITY", out var nationality) || string.IsNullOrWhiteSpace(nationality))
        {
            return;
        }

        fields["NATIONALITY"] = ConvertToTwoLetterCountryCode(nationality);
    }

    private static string? ResolveDocumentType(string? sourceName, string? documentType, IReadOnlyDictionary<string, string> fields)
    {
        // 우선순위: DTO DocumentType > Fields["DOCUMENTTYPE"].
        var rawDocumentType = string.Empty;
        if (!string.IsNullOrWhiteSpace(documentType))
        {
            rawDocumentType = documentType.Trim();
        }
        else if (fields.TryGetValue("DOCUMENTTYPE", out var fromFields) && !string.IsNullOrWhiteSpace(fromFields))
        {
            rawDocumentType = fromFields.Trim();
        }

        if (string.IsNullOrWhiteSpace(rawDocumentType))
        {
            return null;
        }

        return IsExternalSource(sourceName)
            ? MapExternalDocumentType(rawDocumentType)
            : MapInternalDocumentType(rawDocumentType);
    }

    private static bool IsExternalSource(string? sourceName)
        => string.Equals(sourceName?.Trim(), "External", StringComparison.OrdinalIgnoreCase);

    private static string MapInternalDocumentType(string rawDocumentType)
        => rawDocumentType.Trim() switch
        {
            "01" => "02",
            "02" => "01",
            "03" => "03",
            "04" => "04",
            _ => "04"
        };

    private static string MapExternalDocumentType(string rawDocumentType)
        => rawDocumentType.Trim() switch
        {
            "01" or "02" => "03",
            "03" or "04" => "01",
            _ => "04"
        };

    // 특수문자 제거
    private static string RemoveSpecialCharacters(string value)
        => new(value.Where(char.IsLetterOrDigit).ToArray());

    // 국가코드 변환: 3자리(ISO-3) -> 2자리(ISO-2)
    private static string ConvertToTwoLetterCountryCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length == 2)
        {
            return normalized;
        }

        // 3자리 코드면 2자리 코드로 변환.
        if (normalized.Length == 3 && Alpha3ToAlpha2Map.Value.TryGetValue(normalized, out var alpha2))
        {
            return alpha2;
        }

        // 변환 실패 시 원본
        return normalized;
    }

    // 생년월일 계산
    // yymmdd + 주민번호 7번째 자리로 yyyy-MM-dd를 계산한다.
    private static bool TryConvertBirthDate(string yymmdd, char seventhDigit, out string formattedBirthDate)
    {
        formattedBirthDate = string.Empty;
        if (yymmdd.Length != 6 || !yymmdd.All(char.IsDigit))
        {
            return false;
        }

        var yy = int.Parse(yymmdd[..2], CultureInfo.InvariantCulture);
        var mm = int.Parse(yymmdd.Substring(2, 2), CultureInfo.InvariantCulture);
        var dd = int.Parse(yymmdd.Substring(4, 2), CultureInfo.InvariantCulture);

        var year = ResolveBirthYear(yy, seventhDigit);
        if (!DateOnly.TryParseExact(
                $"{year:D4}{mm:D2}{dd:D2}",
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return false;
        }

        formattedBirthDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    // 주민번호 7번째 자리 기준 세기 계산.
    private static int ResolveBirthYear(int twoDigitYear, char seventhDigit)
    {
        var century = seventhDigit switch
        {
            '1' or '2' or '5' or '6' => 1900,
            '3' or '4' or '7' or '8' => 2000,
            '9' or '0' => 1800,
            _ => (DateTime.UtcNow.Year % 100) >= twoDigitYear ? 2000 : 1900
        };

        return century + twoDigitYear;
    }

    private static IReadOnlyDictionary<string, string> CreateCountryCodeMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region;
            try
            {
                region = new RegionInfo(culture.Name);
            }
            catch
            {
                continue;
            }

            var alpha3 = region.ThreeLetterISORegionName?.ToUpperInvariant();
            var alpha2 = region.TwoLetterISORegionName?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(alpha3) || string.IsNullOrWhiteSpace(alpha2))
            {
                continue;
            }

            if (!map.ContainsKey(alpha3))
            {
                map[alpha3] = alpha2;
            }
        }

        return map;
    }
}
