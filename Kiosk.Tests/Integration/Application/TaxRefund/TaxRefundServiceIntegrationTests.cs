using Kiosk.Application.Models.TaxRefund;
using Kiosk.Application.Services.TaxRefund;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Infrastructure.Integrations.Gtf;
using Kiosk.Tests.Common.Fakes;

namespace Kiosk.Tests.Integration.Application.TaxRefund;

public sealed class TaxRefundServiceIntegrationTests
{
    [Fact]
    public async Task InitializeAsync_ReturnsMappedResult_WhenServiceAndClientAreWiredTogether()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","kiosk_no":"K001","kiosk_type":"T1","refund_limit_amt":"500000"}""",
                null,
                "corr-int-tax-init")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var client = new GtfClient(executor, config);
        var sut = new TaxRefundService(client);

        var result = await sut.InitializeAsync("EDI01", "TERM01", "Moneybox");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.KioskNo.Should().Be("K001");
        result.Data.KioskType.Should().Be("T1");
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/operation/initial");
    }

    [Fact]
    public async Task RegisterSlipAsync_ReturnsMappedSummary_WhenServiceAndClientAreWiredTogether()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","passport_serial_no":"PS-01","rows":"2","slip_list":[{"buy_serial_no":"B001","total_refund_amt":"1000"},{"buy_serial_no":"B002","total_refund_amt":"1200"}]}""",
                null,
                "corr-int-tax-register")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var client = new GtfClient(executor, config);
        var sut = new TaxRefundService(client);

        var result = await sut.RegisterSlipAsync(new TaxRefundSlipRegistrationCommand(
            "K001", "T1", "EDI01", "R", "M123", "KR", "PS-01", "QR", "payload"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PassportSerialNo.Should().Be("PS-01");
        result.Data.SlipCount.Should().Be(2);
        result.Data.TotalRefundAmount.Should().Be("1000");
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/trc/registerSlip");
    }
}
