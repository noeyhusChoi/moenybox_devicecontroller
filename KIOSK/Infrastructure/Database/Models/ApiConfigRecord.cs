using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    // TODO: 이전 코드 잔제 정리 필요
    public class ApiConfigRecord
    {
        [Column("SERVER_NAME")]
        public string ServerName { get; set; } = string.Empty;

        [Column("SERVER_URL")]
        public string ServerUrl { get; set; } = string.Empty;

        [Column("SERVER_KEY")]
        public string ServerKey { get; set; } = string.Empty;

        [Column("TIMEOUT_SECONDS")]
        public int TimeoutSeconds { get; set; }
    }
}
