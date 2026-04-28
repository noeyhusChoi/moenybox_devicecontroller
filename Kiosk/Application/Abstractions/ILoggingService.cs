using System;
using System.Runtime.CompilerServices;

namespace Kiosk.Application.Abstractions
{
    public interface ILoggingService
    {
        void Debug(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Info(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Warn(string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");
        void Error(Exception? ex, string message, object[]? args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "");

        IDisposable BeginScope(string name, object value);
    }
}
