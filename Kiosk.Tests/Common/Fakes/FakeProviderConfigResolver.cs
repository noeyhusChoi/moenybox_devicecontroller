using Kiosk.Infrastructure.Integrations.Common;

namespace Kiosk.Tests.Common.Fakes;

internal sealed class FakeProviderConfigResolver : IProviderConfigResolver
{
    private readonly Dictionary<string, ProviderEndpointConfig> _configs = new(StringComparer.OrdinalIgnoreCase);

    public FakeProviderConfigResolver Add(ProviderEndpointConfig config)
    {
        _configs[config.ProviderName] = config;
        return this;
    }

    public ProviderEndpointConfig GetRequired(string providerName)
    {
        if (_configs.TryGetValue(providerName, out var config))
            return config;

        throw new InvalidOperationException($"Missing provider config: {providerName}");
    }
}
