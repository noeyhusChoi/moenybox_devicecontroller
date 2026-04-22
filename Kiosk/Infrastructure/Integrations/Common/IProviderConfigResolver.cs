namespace Kiosk.Infrastructure.Integrations.Common;

public interface IProviderConfigResolver
{
    ProviderEndpointConfig GetRequired(string providerName);
}
