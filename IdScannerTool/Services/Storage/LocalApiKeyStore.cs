using System.IO;
using System.Text.Json;

namespace IdScannerTool.Services;

public sealed class LocalApiKeyStore : IApiKeyStore
{
    private readonly string _filePath;

    public LocalApiKeyStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MBoxIDScanner",
                "api-key.json")
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
            var model = await JsonSerializer.DeserializeAsync<ApiKeyFileModel>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(model?.ApiKey)
                ? null
                : model.ApiKey.Trim();
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key is required.", nameof(apiKey));
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        var model = new ApiKeyFileModel
        {
            ApiKey = apiKey.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        await JsonSerializer.SerializeAsync(stream, model, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed class ApiKeyFileModel
    {
        public string ApiKey { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
