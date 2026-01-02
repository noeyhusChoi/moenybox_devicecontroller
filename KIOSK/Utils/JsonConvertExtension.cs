using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KIOSK.Utils
{
    public static class JsonConvertExtension
    {
        public static string ConvertToJson<T>(T value)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // 보기 좋은 포맷
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // null 값은 생략
                Converters =
                {
                    new JsonStringEnumConverter() // enum을 문자열로 출력 (예: Enum.Down → "Down")
                }
            };

            // 직렬화 실행
            var json = JsonSerializer.Serialize(value, options);
            return json;
        }

        public static T? ConvertFromJson<T>(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 대소문자 구분 없이 매핑
                Converters =
                {
                    new JsonStringEnumConverter() // enum을 문자열로 역직렬화 ("Down" → Enum.Down)
                }
            };

            // 역직렬화 실행
            var result = JsonSerializer.Deserialize<T>(json, options);
            return result;
        }
    }
}
