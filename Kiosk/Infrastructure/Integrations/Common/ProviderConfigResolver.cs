using Kiosk.Infrastructure.Cache;
using Kiosk.Infrastructure.Database.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Kiosk.Infrastructure.Integrations.Common;

public sealed class ProviderConfigResolver : IProviderConfigResolver
{
    private readonly IMemoryCache _cache;

    public ProviderConfigResolver(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ProviderEndpointConfig GetRequired(string providerName)
    {
        var configs = _cache.Get<IReadOnlyList<ApiConfigModel>>(DatabaseCacheKeys.ApiConfigList) ?? Array.Empty<ApiConfigModel>();
        var config = configs.FirstOrDefault(x => string.Equals(x.ServerName, providerName, StringComparison.OrdinalIgnoreCase));
        if (config is null || string.IsNullOrWhiteSpace(config.ServerUrl))
            throw new InvalidOperationException($"Provider config not found: {providerName}");

        return new ProviderEndpointConfig(
            providerName,
            config.ServerUrl,
            config.ServerKey,
            TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 15),
            RetryCount: 1);
    }
}
