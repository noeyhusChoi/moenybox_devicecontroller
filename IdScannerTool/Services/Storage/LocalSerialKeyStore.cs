using System.Text.Json;
using System.IO;

namespace IdScannerTool.Services;

/// <summary>
/// 로컬 시리얼키 저장/조회 서비스.
/// 기본 저장 위치: %LocalAppData%\Moneybox\IdScannerTool\serial-key.json
/// </summary>
public sealed class LocalSerialKeyStore : ILocalSerialKeyStore
{
    private readonly string _filePath;

    public LocalSerialKeyStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Moneybox",
                "IdScannerTool",
                "serial-key.json")
            : filePath;
    }

    public async Task<string?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var model = await JsonSerializer.DeserializeAsync<SerialKeyFileModel>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(model?.SerialKey)
                ? null
                : model.SerialKey.Trim();
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(string serialKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialKey))
        {
            throw new ArgumentException("Serial key is required.", nameof(serialKey));
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        var model = new SerialKeyFileModel
        {
            SerialKey = serialKey.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        await JsonSerializer.SerializeAsync(stream, model, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed class SerialKeyFileModel
    {
        public string SerialKey { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
