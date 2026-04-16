using System.IO;
using Velopack;
using Velopack.Sources;

namespace IdScannerTool.Services;

public interface IAppUpdateService
{
    Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task DownloadAndApplyAsync(
        PendingAppUpdate update,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record PendingAppUpdate(
    string Version,
    string? NotesMarkdown,
    UpdateInfo NativeInfo);

public sealed record AppUpdateCheckResult(
    bool IsConfigured,
    bool IsUpdateAvailable,
    string Message,
    PendingAppUpdate? Update);

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly string _configPath;

    public AppUpdateService(string configPath)
    {
        _configPath = configPath;
    }

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var settings = LoadSettings();
        if (!settings.IsEnabled)
        {
            return new AppUpdateCheckResult(
                false,
                false,
                settings.Reason ?? "업데이트 기능이 비활성화되어 있습니다.",
                null);
        }

        if (string.IsNullOrWhiteSpace(settings.RepoUrl))
        {
            return new AppUpdateCheckResult(false, false, "UPDATE_URL이 설정되지 않았습니다.", null);
        }

        if (!IsGithubRepositoryUrl(settings.RepoUrl))
        {
            return new AppUpdateCheckResult(false, false, "UPDATE_URL은 GitHub 저장소 URL이어야 합니다.", null);
        }

        var manager = CreateManager(settings);
        var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (update is null)
        {
            return new AppUpdateCheckResult(true, false, "최신 버전입니다.", null);
        }

        var version = update.TargetFullRelease?.Version?.ToString() ?? "알 수 없는 버전";
        var notes = update.TargetFullRelease?.NotesMarkdown;
        return new AppUpdateCheckResult(
            true,
            true,
            $"새 버전 {version} 이(가) 있습니다.",
            new PendingAppUpdate(version, notes, update));
    }

    public async Task DownloadAndApplyAsync(
        PendingAppUpdate update,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var settings = LoadSettings();
        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.RepoUrl))
        {
            throw new InvalidOperationException("업데이트 설정이 올바르지 않습니다.");
        }

        var manager = CreateManager(settings);
        await manager.DownloadUpdatesAsync(update.NativeInfo, progress).ConfigureAwait(false);
        manager.ApplyUpdatesAndRestart(update.NativeInfo);
    }

    private static bool IsGithubRepositoryUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
           && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
           && uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Length >= 2;

    private static UpdateManager CreateManager(AppUpdateSettings settings)
    {
        var options = string.IsNullOrWhiteSpace(settings.Channel)
            ? null
            : new UpdateOptions { ExplicitChannel = settings.Channel.Trim() };
        var source = new GithubSource(settings.RepoUrl!, accessToken: null, prerelease: false);
        return new UpdateManager(source, options);
    }

    private AppUpdateSettings LoadSettings()
    {
        if (!File.Exists(_configPath))
        {
            return AppUpdateSettings.Disabled("Config.ini 파일을 찾을 수 없습니다.");
        }

        var currentSection = string.Empty;
        string? updateUrl = null;
        string? updateDisable = null;
        string? updateChannel = null;

        foreach (var rawLine in File.ReadLines(_configPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                continue;
            }

            if (!string.Equals(currentSection, "GENERAL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            switch (key.ToUpperInvariant())
            {
                case "UPDATE_URL":
                    updateUrl = value;
                    break;
                case "UPDATE_DISABLE":
                    updateDisable = value;
                    break;
                case "UPDATE_CHANNEL":
                    updateChannel = value;
                    break;
            }
        }

        if (string.Equals(updateDisable, "1", StringComparison.OrdinalIgnoreCase))
        {
            return AppUpdateSettings.Disabled("Config.ini에서 업데이트가 비활성화되어 있습니다.");
        }

        return new AppUpdateSettings(
            IsEnabled: true,
            RepoUrl: string.IsNullOrWhiteSpace(updateUrl) ? null : updateUrl,
            Channel: string.IsNullOrWhiteSpace(updateChannel) ? null : updateChannel,
            Reason: null);
    }

    private sealed record AppUpdateSettings(
        bool IsEnabled,
        string? RepoUrl,
        string? Channel,
        string? Reason)
    {
        public static AppUpdateSettings Disabled(string reason)
            => new(false, null, null, reason);
    }
}
