using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace KIOSK.Domain.Entities
{
    public partial class GtfTaxRefundModel : ObservableObject
    {
        public Guid SessionId { get; } = Guid.NewGuid();

        // 공통 정보 (initial)
        public string? Edi { get; set; }
        public string? KioskNo { get; set; }
        public string? KioskType { get; set; }
        public string? RefundLimitAmt { get; set; }

        // 신분증 정보 (inquirySlipList 요청/응답)
        public string? Name { get; set; }
        public string? PassportNo { get; set; }
        public string? NationalityCode { get; set; }
        public string? Birthday { get; set; }
        public string? PassportExpirdate { get; set; }
        public string? GenderCode { get; set; }
        public string? InputWayCode { get; set; }
        public string? PassportSerialNo { get; set; }   // 응답에서 받는 값

        // QR로 등록/검증된 슬립 리스트 (registerSlip 응답)
        public ObservableCollection<GtfSlipItem> SlipItems { get; } = new();

        // 환불 종류/방식 + 결과
        public string? RefundTypeCode { get; set; }      // 환불유형 : 01 현금, 02 송금(고정)
        public string? RefundWayCode { get; set; }       // 환불수단 코드 : 01 현금, 02 카드, 05 알리페이, 18 위챗
        public string? RefundNo { get; set; }            // 최종 refund_no
        public string? TotalRefundAmt { get; set; }      // 총 환불금액(필요시)
        public string? TotalDepositAmt { get; set; }     // 입금형이면 deposit_amt

        // 알리페이 계정
        public ObservableCollection<AlipayUser> AlipayUsers { get; } = new();

        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // 총 합산
        [ObservableProperty] public decimal totalBuyAmtSum;
        [ObservableProperty] public decimal totalRefundAmtSum;

        // 선택 환급 수단
        public string? SelectedRefundWayCode { get; set; }
    }

    public sealed class AlipayUser
    {
        public string? UserName { get; set; }
        public string? UserId { get; set; }
        public string? LoginId { get; set; }
    }

    public sealed class GtfSlipItem
    {
        public string? QrData { get; set; }
        public string? BuySerialNo { get; set; }
        public string? SellDate { get; set; }
        public string? SellTime { get; set; }
        public string? TotalBuyAmt { get; set; }
        public string? TotalRefundAmt { get; set; }
        public string? Qty { get; set; }
        public string? TotalTaxAmt { get; set; }
        public string? SlipStatusCode { get; set; }
        public string? HotelRefundYn { get; set; }
        public string? MediRefundYn { get; set; }
    }
}
