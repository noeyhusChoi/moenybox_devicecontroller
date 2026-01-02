namespace KIOSK.Infrastructure.Configuration
{
    /// <summary>
    /// 애플리케이션 공용 경로를 담는 설정 객체. 설정 파일 바인딩 또는 코드로 초기화해 DI로 주입.
    /// </summary>
    public sealed class AppPaths
    {
        public string DbPath { get; init; } = string.Empty;
        public string AssetsRoot { get; init; } = string.Empty;
        public string LogPath { get; init; } = string.Empty;
        public string OcrDir { get; init; } = string.Empty;
    }
}
