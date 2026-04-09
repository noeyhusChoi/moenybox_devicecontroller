using System.Text.Json;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Infrastructure.Integrations.Gtf;
using Kiosk.Infrastructure.Integrations.Gtf.Models;
using Kiosk.Infrastructure.Integrations.Gtf.Requests;
using Kiosk.Infrastructure.Integrations.Gtf.Responses;
using Kiosk.Tests.Common.Fakes;

namespace Kiosk.Tests.Unit.Infrastructure.Integrations.Gtf;

public sealed class GtfClientTests
{
    [Fact]
    public async Task InitialAsync_BuildsJsonRequestAndParsesSuccessResponse()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","kiosk_no":"K001","kiosk_type":"T1","refund_limit_amt":"500000"}""",
                null,
                "corr-gtf-init")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 3));
        var sut = new GtfClient(executor, config);

        var result = await sut.InitialAsync(new GtfInitialRequest
        {
            Edi = "EDI01",
            TmlId = "TERM01",
            ShopName = "Moneybox"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.KioskNo.Should().Be("K001");
        executor.LastMethod.Should().Be(HttpMethod.Post);
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/operation/initial");
        var body = executor.LastRequestBody;
        body.Should().NotBeNull();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("edi").GetString().Should().Be("EDI01");
        json.RootElement.GetProperty("tml_id").GetString().Should().Be("TERM01");
        json.RootElement.GetProperty("shop_name").GetString().Should().Be("Moneybox");
    }

    [Fact]
    public async Task InitialAsync_ReturnsProviderErrorWhenRcIsNotSuccess()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"1001","rm":"INVALID","kiosk_no":null,"kiosk_type":null,"refund_limit_amt":null}""",
                null,
                "corr-gtf-error")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 0));
        var sut = new GtfClient(executor, config);

        var result = await sut.InitialAsync(new GtfInitialRequest());

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("1001");
        result.Error.Message.Should().Be("INVALID");
    }

    [Fact]
    public async Task RegisterSlipAsync_BuildsJsonRequestAndParsesSlipList()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","passport_serial_no":"PS-01","rows":"2","slip_list":[{"buy_serial_no":"B001","total_refund_amt":"1000"},{"buy_serial_no":"B002","total_refund_amt":"1200"}]}""",
                null,
                "corr-gtf-register")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.RegisterSlipAsync(new GtfRegisterSlipRequest
        {
            KioskNo = "K001",
            KioskType = "T1",
            Edi = "EDI01",
            RefundTypeCode = "R",
            PassportNo = "M123",
            NationalityCode = "KR",
            PassportSerialNo = "PS-01",
            QrDataType = "QR",
            QrData = "payload"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PassportSerialNo.Should().Be("PS-01");
        result.Data.List.Count.Should().Be(2);
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/trc/registerSlip");
        executor.LastRequestBody.Should().NotBeNull();
        using var json = JsonDocument.Parse(executor.LastRequestBody);
        json.RootElement.GetProperty("kiosk_no").GetString().Should().Be("K001");
        json.RootElement.GetProperty("qr_data").GetString().Should().Be("payload");
    }

    [Fact]
    public async Task RegisterSlipAsync_ReturnsProviderErrorWhenRcIsNotSuccess()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"2001","rm":"SLIP_INVALID","passport_serial_no":null,"rows":"0","slip_list":[]}""",
                null,
                "corr-gtf-register-fail")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.RegisterSlipAsync(new GtfRegisterSlipRequest());

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("2001");
        result.Error.Message.Should().Be("SLIP_INVALID");
    }

    [Fact]
    public async Task InquirySlipListAsync_BuildsJsonRequestAndParsesResponse()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","passport_serial_no":"PS-FOUND"}""",
                null,
                "corr-gtf-inquiry")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.InquirySlipListAsync(new GtfInquirySlipListRequest
        {
            KioskNo = "K001",
            KioskType = "T1",
            Name = "Kim",
            PassportNo = "M123",
            NationalityCode = "KR",
            Birthday = "19900101",
            PassportExpirdate = "20300101",
            GenderCode = "M"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PassportSerialNo.Should().Be("PS-FOUND");
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/trc/inquirySlipList");
    }

    [Fact]
    public async Task PossibilityAsync_ParsesRefundNumberAndSerials()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","refund_no":"R001","buy_serial_no":["B001","B002"]}""",
                null,
                "corr-gtf-possibility")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.PossibilityAsync(new GtfPossibilityRequest
        {
            KioskNo = "K001",
            KioskType = "T1",
            Edi = "EDI01",
            RefundTypeCode = "R",
            RefundNo = "R001",
            BuySerialNo = ["B001", "B002"],
            NumberOfSlip = "2"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefundNo.Should().Be("R001");
        result.Data.BuySerialNo!.Length.Should().Be(2);
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/trc/possibility");
    }

    [Fact]
    public async Task RollbackAsync_BuildsJsonRequestAndParsesSuccess()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"ROLLBACK_OK"}""",
                null,
                "corr-gtf-rollback")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.RollbackAsync(new GtfRollbackRequest
        {
            KioskNo = "K001",
            KioskType = "T1",
            Edi = "EDI01",
            RefundTypeCode = "R",
            RefundWayCode = "CARD",
            RefundNo = "R001",
            BuySerialNo = ["B001"],
            NumberOfSlip = "1"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Rm.Should().Be("ROLLBACK_OK");
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/trc/rollback");
    }

    [Fact]
    public async Task DepositAmountAsync_BuildsJsonRequestAndParsesAmount()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"rc":"0000","rm":"OK","deposit_amt":"50000"}""",
                null,
                "corr-gtf-deposit")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("GTF", "https://gtf.test", "", TimeSpan.FromSeconds(8), 1));
        var sut = new GtfClient(executor, config);

        var result = await sut.DepositAmountAsync(new GtfDepositAmountRequest
        {
            KioskNo = "K001",
            KioskType = "T1",
            Edi = "EDI01",
            RefundTypeCode = "R",
            BuySerialNo = ["B001", "B002"],
            NumberOfSlip = "2"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.DepositAmt.Should().Be("50000");
        executor.LastRequestUri!.ToString().Should().Be("https://gtf.test/refund/depositAmt");
    }
}
