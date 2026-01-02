using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace KIOSK.Infrastructure.Logging
{
    public interface ILoggingService
    {
        void Debug(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Info(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Warn(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Error(Exception? ex, string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");

        /// <summary>스코프(프로퍼티)를 사용하여 공통 속성 추가</summary>
        IDisposable BeginScope(string name, object value);
    }

    public sealed class LoggingService : ILoggingService, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed = false;

        public LoggingService()
        {
            _logger = Init();

            // 전역 로거에도 설정 (옵션)
            Log.Logger = _logger;
        }

        private Logger Init([CallerFilePath] string file = "")
        {
            var basePath = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var directoryName = "Logs"; // 고정

            // 폴더 생성
            var directoryPath = Path.Combine(basePath, directoryName);
            Directory.CreateDirectory(directoryPath);

            // 파일 이름 접두사
            var filePrefix = $"m24h_log_.log";

            // 전체 경로 ( 폴더 + 파일 )
            var fullPath = Path.Combine(directoryPath, filePrefix);

            // 파일 생성 단위 (* Day 고정)
            var fileRollingInterval = RollingInterval.Day;

            // 파일 저장 제한 (최대 1일, 30일, 365일..)
            int fileRetentionPeriod = 30;

            // 파일당 사이즈 (10MB, 1GB..)
            var fileMaximumSize = "100MB";

            // 로그 설정
            var lc = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(new LoggingLevelSwitch(LogEventLevel.Verbose))
                .Enrich.FromLogContext();

            if (true) // 현재 = 파일 저장 (DB, 서버 전송 확장 가능)
            {
                lc.WriteTo.File
                (
                    formatter: new CompactJsonFormatter(),
                    path: fullPath,                                                                 // 로그 생성 경로
                    retainedFileTimeLimit: TimeSpan.FromDays(fileRetentionPeriod),                  // 로그 저장 제한 (날짜 기준)
                    fileSizeLimitBytes: ParseSizeToBytes(fileMaximumSize),                          // 로그 최대 크기
                    rollingInterval: fileRollingInterval,                                           // 로그 생성 단위 (1일 고정)
                    rollOnFileSizeLimit: true,
                    shared: true
                );
            }

#if DEBUG
            lc.WriteTo.Debug();
            lc.WriteTo.Seq("http://localhost:5341", apiKey: "l9RG3NsYsflCV22Dpkr5");
#endif

            return lc.CreateLogger();
        }


        public void Debug(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            var logger = _logger
                .ForContext("SourceFile", $"{Path.GetFileName(file)}:{line}")
                .ForContext("Member", $"{member}()");
            
            if (args == null || args.Length == 0) logger.Debug(message);
            else logger.Debug(message, args);
        }
        public void Info(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            var logger = _logger
                .ForContext("SourceFile", $"{Path.GetFileName(file)}:{line}")
                .ForContext("Member", $"{member}()");

            if (args == null || args.Length == 0) logger.Information(message);
            else logger.Information(message, args);
        }
        public void Warn(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            var logger = _logger
                .ForContext("SourceFile", $"{Path.GetFileName(file)}:{line}")
                .ForContext("Member", $"{member}()");

            if (args == null || args.Length == 0) logger.Warning(message);
            else logger.Warning(message, args);
        }
        public void Error(Exception? ex, string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            var logger = _logger
                .ForContext("SourceFile", $"{Path.GetFileName(file)}:{line}")
                .ForContext("Member", $"{member}()");

            if (ex is not null)
            {
                if (args == null || args.Length == 0) logger.Error(ex, message);
                else logger.Error(ex, message, args);
            }
            else
            {
                if (args == null || args.Length == 0) logger.Error(message);
                else logger.Error(message, args);
            }
        }

        /// <summary>
        /// using(var scope = logger.BeginScope("KioskId", "k-123")) { ... }
        /// </summary>
        public IDisposable BeginScope(string name, object value) => LogContext.PushProperty(name, value);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Serilog flush
            Log.CloseAndFlush();
            if (_logger is IDisposable d) d.Dispose();
        }

        #region Util
        private static LogEventLevel ParseLevel(string level) =>
        level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        private static long ParseSizeToBytes(string size)
        {
            if (string.IsNullOrWhiteSpace(size))
                throw new ArgumentException("size is null or empty", nameof(size));

            var s = size.Trim().ToUpperInvariant();

            decimal ParseNumber(string v) => decimal.Parse(v, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

            return s switch
            {
                var x when x.EndsWith("TIB") => (long)(ParseNumber(x[..^3]) * (decimal)(1L << 40)),
                var x when x.EndsWith("TB") => (long)(ParseNumber(x[..^2]) * (decimal)(1L << 40)),
                var x when x.EndsWith("GIB") => (long)(ParseNumber(x[..^3]) * (decimal)(1L << 30)),
                var x when x.EndsWith("GB") => (long)(ParseNumber(x[..^2]) * (decimal)(1L << 30)),
                var x when x.EndsWith("MIB") => (long)(ParseNumber(x[..^3]) * (decimal)(1L << 20)),
                var x when x.EndsWith("MB") => (long)(ParseNumber(x[..^2]) * (decimal)(1L << 20)),
                var x when x.EndsWith("KIB") => (long)(ParseNumber(x[..^3]) * (decimal)(1L << 10)),
                var x when x.EndsWith("KB") => (long)(ParseNumber(x[..^2]) * (decimal)(1L << 10)),
                var x when x.EndsWith("B") && x.Length > 1 => (long)ParseNumber(x[..^1]),
                var x => long.Parse(x) // raw bytes
            };
        }
        #endregion
    }
}
