using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class ApiConfigRecord
    {
        [Column("SERVER_NAME")]
        public string ServerName { get; set; }

        [Column("SERVER_URL")]
        public string ServerUrl { get; set; }

        [Column("SERVER_KEY")]
        public string ServerKey { get; set; }

        [Column("TIMEOUT_SECONDS")]
        public int TimeoutSeconds { get; set; }
    }
}
