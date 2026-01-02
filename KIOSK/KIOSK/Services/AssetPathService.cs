using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KIOSK.Services
{
 
    // TODO: 매니페스트 방식 사용 안할 예정, 삭제 필요
    // 파일 경로 초기화 시 파일 존재 여부 확인 및 로그 출력

    public enum AssetKey
    {
        Logo,
        IconClose,
        HeroImage,
        IntroVideo
    }

    public interface IAssetPathService
    {
        string? GetPath(AssetKey key);
        bool TryGetPath(AssetKey key, out string? path);
        void RegisterOverride(AssetKey key, string path);
        bool RemoveOverride(AssetKey key);
        Task LoadManifestAsync(string manifestPath, CancellationToken ct = default);
        void SaveManifest(string manifestPath);
        event EventHandler<AssetPathsChangedEventArgs>? PathsChanged;
        IReadOnlyDictionary<AssetKey, string> GetAllPathsSnapshot();
    }

    public class AssetPathsChangedEventArgs : EventArgs
    {
        public IReadOnlyList<AssetKey> ChangedKeys { get; }
        public AssetPathsChangedEventArgs(IReadOnlyList<AssetKey> keys) => ChangedKeys = keys;
    }

    public class AssetPathService : IAssetPathService, IDisposable
    {
        private readonly ConcurrentDictionary<AssetKey, string> _map = new();
        private FileSystemWatcher? _watcher;
        private string? _manifestPath;

        public event EventHandler<AssetPathsChangedEventArgs>? PathsChanged;

        public AssetPathService()
        {
            // 기본값 초기화: 필요하면 DI로 주입하거나 별도 초기화 호출
            _map[AssetKey.Logo] = "pack://application:,,,/Assets/logo.png";
            // ... 기타 초기값
        }

        public string? GetPath(AssetKey key) => _map.TryGetValue(key, out var p) ? p : null;
        public bool TryGetPath(AssetKey key, out string? path) => _map.TryGetValue(key, out path);

        public void RegisterOverride(AssetKey key, string path)
        {
            _map.AddOrUpdate(key, path, (_, __) => path);
            PathsChanged?.Invoke(this, new AssetPathsChangedEventArgs(new[] { key }));
        }

        public bool RemoveOverride(AssetKey key)
        {
            var ok = _map.TryRemove(key, out _);
            if (ok) PathsChanged?.Invoke(this, new AssetPathsChangedEventArgs(new[] { key }));
            return ok;
        }

        public async Task LoadManifestAsync(string manifestPath, CancellationToken ct = default)
        {
            if (!File.Exists(manifestPath)) throw new FileNotFoundException(manifestPath);
            _manifestPath = manifestPath;

            using var fs = File.OpenRead(manifestPath);
            var map = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(fs, cancellationToken: ct);
            if (map == null) return;

            var changed = new List<AssetKey>();
            foreach (var kv in map)
            {
                if (Enum.TryParse<AssetKey>(kv.Key, true, out var key))
                {
                    _map.AddOrUpdate(key, kv.Value, (_, __) => kv.Value);
                    changed.Add(key);
                }
            }
            if (changed.Count > 0) PathsChanged?.Invoke(this, new AssetPathsChangedEventArgs(changed));

            StartWatchManifestFile(); // 핫리로드 활성화
        }

        public void SaveManifest(string manifestPath)
        {
            var dict = _map.ToDictionary(k => k.Key.ToString(), v => v.Value);
            var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, json);
            _manifestPath = manifestPath;
            StartWatchManifestFile();
        }

        private void StartWatchManifestFile()
        {
            if (string.IsNullOrEmpty(_manifestPath)) return;
            try
            {
                _watcher?.Dispose();
                _watcher = new FileSystemWatcher(Path.GetDirectoryName(_manifestPath) ?? ".", Path.GetFileName(_manifestPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _watcher.Changed += async (_, __) =>
                {
                    // 짧게 딜레이(쓰기 완료 대기) 후 재로드
                    await Task.Delay(200);
                    try { await LoadManifestAsync(_manifestPath!); } catch { /* 로깅 */ }
                }; 
                _watcher.EnableRaisingEvents = true;
            }
            catch { /* 로깅 */ }
        }

        public IReadOnlyDictionary<AssetKey, string> GetAllPathsSnapshot() =>
            new Dictionary<AssetKey, string>(_map);

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
