using Kiosk.Application.Services.TaxRefund;
using Kiosk.Infrastructure.Integrations.Gtf;
using Kiosk.Infrastructure.Integrations.Gtf.Models;
using Kiosk.Infrastructure.Integrations.Gtf.Requests;
using Kiosk.Infrastructure.Integrations.Gtf.Responses;

namespace Kiosk.Tests.Unit.Application.TaxRefund;

public sealed class TaxRefundServiceTests
{
    [Fact]
    public async Task InitializeAsync_MapsClientResponseToOperationResult()
    {
        var client = Substitute.For<IGtfClient>();
        client.InitialAsync(Arg.Any<GtfInitialRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfInitialResponse>(
                true,
                new GtfInitialResponse
                {
                    Rc = "0000",
                    Rm = "OK",
                    KioskNo = "K100",
                    KioskType = "TYPE-A",
                    RefundLimitAmt = "300000"
                },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-tax-init")));
        var sut = new TaxRefundService(client);

        var result = await sut.InitializeAsync("EDI", "TERM", "SHOP");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.KioskNo.Should().Be("K100");
        result.Data.KioskType.Should().Be("TYPE-A");
        result.Data.RefundLimitAmount.Should().Be("300000");
        result.Provider.Should().Be("GTF");
        await client.Received(1).InitialAsync(
            Arg.Is<GtfInitialRequest>(x => x.Edi == "EDI" && x.TmlId == "TERM" && x.ShopName == "SHOP"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_MapsClientFailureToAppError()
    {
        var client = Substitute.For<IGtfClient>();
        client.InitialAsync(Arg.Any<GtfInitialRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfInitialResponse>(
                false,
                null,
                new GtfError("1001", "INVALID", false),
                200,
                """{"rc":"1001","rm":"INVALID"}""",
                "corr-tax-error")));
        var sut = new TaxRefundService(client);

        var result = await sut.InitializeAsync("EDI", "TERM", "SHOP");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("1001");
        result.Error.Provider.Should().Be("GTF");
        result.Error.Retryable.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterSlipAsync_MapsSlipSummaryToAppResult()
    {
        var client = Substitute.For<IGtfClient>();
        client.RegisterSlipAsync(Arg.Any<GtfRegisterSlipRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfRegisterSlipResponse>(
                true,
                new GtfRegisterSlipResponse
                {
                    Rc = "0000",
                    Rm = "OK",
                    PassportSerialNo = "PS-01",
                    Rows = "2",
                    List =
                    [
                        new GtfRegisterSlipItem { BuySerialNo = "B001", TotalRefundAmt = "1000" },
                        new GtfRegisterSlipItem { BuySerialNo = "B002", TotalRefundAmt = "1200" }
                    ]
                },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-register-slip")));
        var sut = new TaxRefundService(client);

        var result = await sut.RegisterSlipAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundSlipRegistrationCommand(
            "K001", "T1", "EDI01", "R", "M123", "KR", "PS-01", "QR", "payload"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PassportSerialNo.Should().Be("PS-01");
        result.Data.SlipCount.Should().Be(2);
        result.Data.TotalRefundAmount.Should().Be("1000");
        result.Provider.Should().Be("GTF");
        await client.Received(1).RegisterSlipAsync(
            Arg.Is<GtfRegisterSlipRequest>(x => x.KioskNo == "K001" && x.QrData == "payload"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterSlipAsync_MapsFailureToAppError()
    {
        var client = Substitute.For<IGtfClient>();
        client.RegisterSlipAsync(Arg.Any<GtfRegisterSlipRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfRegisterSlipResponse>(
                false,
                new GtfRegisterSlipResponse
                {
                    Rc = "2001",
                    Rm = "SLIP_INVALID",
                    PassportSerialNo = null,
                    Rows = "0",
                    List = []
                },
                new GtfError("2001", "SLIP_INVALID", false),
                200,
                """{"rc":"2001","rm":"SLIP_INVALID"}""",
                "corr-register-slip-fail")));
        var sut = new TaxRefundService(client);

        var result = await sut.RegisterSlipAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundSlipRegistrationCommand(
            "K001", "T1", "EDI01", "R", "M123", "KR", "PS-01", "QR", "payload"));

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("2001");
        result.Error.Message.Should().Be("SLIP_INVALID");
        result.Error.Provider.Should().Be("GTF");
    }

    [Fact]
    public async Task LookupCustomerAsync_MapsPassportSerialNumber()
    {
        var client = Substitute.For<IGtfClient>();
        client.InquirySlipListAsync(Arg.Any<GtfInquirySlipListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfInquirySlipListResponse>(
                true,
                new GtfInquirySlipListResponse
                {
                    Rc = "0000",
                    Rm = "OK",
                    PassportSerialNo = "PS-FOUND"
                },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-lookup")));
        var sut = new TaxRefundService(client);

        var result = await sut.LookupCustomerAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundCustomerLookupQuery(
            "K001", "T1", "Kim", "M123", "KR", "19900101", "20300101", "M"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PassportSerialNo.Should().Be("PS-FOUND");
        result.Provider.Should().Be("GTF");
        await client.Received(1).InquirySlipListAsync(
            Arg.Is<GtfInquirySlipListRequest>(x => x.KioskNo == "K001" && x.PassportNo == "M123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckEligibilityAsync_MapsRefundNumberAndSerials()
    {
        var client = Substitute.For<IGtfClient>();
        client.PossibilityAsync(Arg.Any<GtfPossibilityRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfPossibilityResponse>(
                true,
                new GtfPossibilityResponse
                {
                    Rc = "0000",
                    Rm = "OK",
                    RefundNo = "R001",
                    BuySerialNo = ["B001", "B002"]
                },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-eligibility")));
        var sut = new TaxRefundService(client);

        var result = await sut.CheckEligibilityAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundEligibilityQuery(
            "K001", "T1", "EDI01", "R", "R001", ["B001", "B002"], "2"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefundNo.Should().Be("R001");
        result.Data.BuySerialNos.Count.Should().Be(2);
        result.Provider.Should().Be("GTF");
        await client.Received(1).PossibilityAsync(
            Arg.Is<GtfPossibilityRequest>(x => x.RefundNo == "R001" && x.NumberOfSlip == "2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_MapsSuccessToOperationResult()
    {
        var client = Substitute.For<IGtfClient>();
        client.RollbackAsync(Arg.Any<GtfRollbackRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfRollbackResponse>(
                true,
                new GtfRollbackResponse { Rc = "0000", Rm = "ROLLBACK_OK" },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-rollback")));
        var sut = new TaxRefundService(client);

        var result = await sut.RollbackAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundRollbackCommand(
            "K001", "T1", "EDI01", "R", "CARD", "R001", ["B001"], "1"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolledBack.Should().BeTrue();
        result.Provider.Should().Be("GTF");
        await client.Received(1).RollbackAsync(
            Arg.Is<GtfRollbackRequest>(x => x.RefundNo == "R001" && x.NumberOfSlip == "1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDepositAmountAsync_MapsAmountToOperationResult()
    {
        var client = Substitute.For<IGtfClient>();
        client.DepositAmountAsync(Arg.Any<GtfDepositAmountRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GtfApiResult<GtfDepositAmountResponse>(
                true,
                new GtfDepositAmountResponse { Rc = "0000", Rm = "OK", DepositAmt = "50000" },
                null,
                200,
                """{"rc":"0000"}""",
                "corr-deposit")));
        var sut = new TaxRefundService(client);

        var result = await sut.GetDepositAmountAsync(new Kiosk.Application.Models.TaxRefund.TaxRefundDepositAmountQuery(
            "K001", "T1", "EDI01", "R", ["B001", "B002"], "2"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.DepositAmount.Should().Be("50000");
        result.Provider.Should().Be("GTF");
        await client.Received(1).DepositAmountAsync(
            Arg.Is<GtfDepositAmountRequest>(x => x.KioskNo == "K001" && x.NumberOfSlip == "2"),
            Arg.Any<CancellationToken>());
    }
}
