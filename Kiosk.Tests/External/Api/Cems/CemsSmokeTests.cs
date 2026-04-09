using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace Kiosk.Tests.External.Api.Cems;

public sealed class CemsSmokeTests
{
    private readonly ITestOutputHelper _output;

    public CemsSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetRateAsync_CallsRealCems_WhenEnvironmentIsConfigured()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CEMS_TEST_BASE_URL");
        var apiKey = Environment.GetEnvironmentVariable("CEMS_TEST_API_KEY");
        var currency = Environment.GetEnvironmentVariable("CEMS_TEST_CURRENCY") ?? "USD";

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _output.WriteLine("CEMS external smoke test skipped. Set CEMS_TEST_BASE_URL and CEMS_TEST_API_KEY.");
            return;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient("ExternalApi");
        services.AddSingleton<IHttpExecutor, HttpExecutor>();
        services.AddSingleton<IProviderConfigResolver>(new ExternalCemsConfigResolver(baseUrl, apiKey));
        services.AddScoped<ICemsClient, CemsClient>();

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICemsClient>();

        var result = await client.GetRateAsync(new CemsGetRateRequest(currency));

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
        result.Data!.CurrencyCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetRateAllAsync_CallsRealCems_WhenEnvironmentIsConfigured()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CEMS_TEST_BASE_URL");
        var apiKey = Environment.GetEnvironmentVariable("CEMS_TEST_API_KEY");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _output.WriteLine("CEMS external smoke test skipped. Set CEMS_TEST_BASE_URL and CEMS_TEST_API_KEY.");
            return;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient("ExternalApi");
        services.AddSingleton<IHttpExecutor, HttpExecutor>();
        services.AddSingleton<IProviderConfigResolver>(new ExternalCemsConfigResolver(baseUrl, apiKey));
        services.AddScoped<ICemsClient, CemsClient>();

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICemsClient>();

        var result = await client.GetRateAllAsync(new CemsGetRateAllRequest());

        _output.WriteLine($"Success: {result.Success}");
        _output.WriteLine($"CorrelationId: {result.CorrelationId}");
        _output.WriteLine($"RawBody: {result.RawBody}");
        if (result.Data is not null)
            _output.WriteLine($"Rates: {JsonSerializer.Serialize(result.Data.Rates)}");
        if (result.Error is not null)
            _output.WriteLine($"Error: {JsonSerializer.Serialize(result.Error)}");

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Rates.Should().NotBeEmpty();
    }

    private sealed class ExternalCemsConfigResolver : IProviderConfigResolver
    {
        private readonly ProviderEndpointConfig _config;

        public ExternalCemsConfigResolver(string baseUrl, string apiKey)
        {
            _config = new ProviderEndpointConfig(
                "CEMS",
                baseUrl,
                apiKey,
                TimeSpan.FromSeconds(15),
                0);
        }

        public ProviderEndpointConfig GetRequired(string providerName)
        {
            if (!string.Equals(providerName, "CEMS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsupported provider for smoke test: {providerName}");

            return _config;
        }
    }
}
