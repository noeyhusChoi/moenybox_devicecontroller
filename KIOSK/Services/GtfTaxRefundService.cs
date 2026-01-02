using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Models;
using System.Diagnostics;

namespace KIOSK.Services
{
    public interface IGtfTaxRefundService
    {
        GtfTaxRefundModel Current { get; }

        void Reset();

        void ApplyInitialResponse(InitialRequestDto req, InitialResponseDto resp);
        void ApplyInquirySlipList(InquirySlipListRequestDto req, InquirySlipListResponseDto resp);
        void AddSlip(RegisterSlipRequestDto req, RegisterSlipResponseDto resp);

        // 최종 환불 타입별 적용
        void ApplyCardRefund(CardRefundRequestDto req, CardRefundResponseDto resp);
        void ApplyAlipayRefund(AlipayRefundRequestDto req, AlipayRefundResponseDto resp);
        void ApplyWechatRefund(WechatRefundRequestDto req, WechatRefundResponseDto resp);
        void ApplyDepositAmt(DepositAmtRequestDto req, DepositAmtResponseDto resp);

        // 알리 페이 계정
        void ApplyAlipayAccount(AlipayConfirmRequestDto req, AlipayConfirmResponseDto res);

        //GtfTransactionEntity ToEntity(); // DB에 저장할 엔티티로 변환
    }

    class GtfTaxRefundService : IGtfTaxRefundService
    {
        public GtfTaxRefundModel Current { get; private set; } = new();

        public void Reset()
        {
            Current = new GtfTaxRefundModel();
        }

        public void ApplyInitialResponse(InitialRequestDto req, InitialResponseDto resp)
        {
            Current.Edi = req.Edi;
            Current.KioskNo = resp.KioskNo;
            Current.KioskType = resp.KioskType;
            Current.RefundLimitAmt = resp.RefundLimitAmt;
        }

        public void ApplyInquirySlipList(InquirySlipListRequestDto req, InquirySlipListResponseDto resp)
        {
            Current.Name = req.Name;
            Current.PassportNo = req.PassportNo;
            Current.NationalityCode = req.NationalityCode;
            Current.Birthday = req.Birthday;
            Current.PassportExpirdate = req.PassportExpirdate;
            Current.GenderCode = req.GenderCode;
            Current.InputWayCode = req.InputWayCode;

            Current.PassportSerialNo = resp.PassportSerialNo;
        }

        public void AddSlip(RegisterSlipRequestDto req, RegisterSlipResponseDto resp)
        {
            // 1) 중복 전표 확인
            if (Current.SlipItems.Any(x => x.QrData == req.QrData))
            {
                // TODO : 해당 로직 구현 및 메세지박스 구현
                Trace.WriteLine("동일 전표");
            }

            foreach (var item in resp.List)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 1) 동일 BuySerialNo 존재 여부 체크
                    bool exists = Current.SlipItems.Any(x => x.BuySerialNo == item.BuySerialNo);

                    // 2) 없을 때만 추가
                    if (!exists)
                    {
                        Current.SlipItems.Add(new GtfSlipItem
                        {
                            QrData = req.QrData,
                            BuySerialNo = item.BuySerialNo,
                            SellDate = item.SellDate,
                            SellTime = item.SellTime,
                            TotalBuyAmt = item.TotalBuyAmt,
                            TotalRefundAmt = item.TotalRefundAmt,
                            Qty = item.Qty,
                            TotalTaxAmt = item.TotalTaxAmt,
                            SlipStatusCode = item.SlipStatusCode,
                            HotelRefundYn = item.HotelRefundYn,
                            MediRefundYn = item.MediRefundYn
                        });

                        RecalculateTotals();
                    }
                });
            }
        }

        public void ApplyCardRefund(CardRefundRequestDto req, CardRefundResponseDto resp)
        {
            Current.RefundTypeCode = req.RefundTypeCode;
            Current.RefundWayCode = req.RefundWayCode;
            Current.RefundNo = resp.RefundNo;
        }

        public void ApplyAlipayRefund(AlipayRefundRequestDto req, AlipayRefundResponseDto resp)
        {
            Current.RefundTypeCode = req.RefundTypeCode;
            Current.RefundWayCode = req.RefundWayCode;
            Current.RefundNo = resp.RefundNo;
        }

        public void ApplyWechatRefund(WechatRefundRequestDto req, WechatRefundResponseDto resp)
        {
            Current.RefundTypeCode = req.RefundTypeCode;
            Current.RefundWayCode = req.RefundWayCode;
            Current.RefundNo = resp.RefundNo;
            Current.TotalRefundAmt = resp.TotalWechatRefundAmt;
        }

        public void ApplyDepositAmt(DepositAmtRequestDto req, DepositAmtResponseDto resp)
        {
            Current.TotalDepositAmt = resp.DepositAmt;
        }

        public void ApplyAlipayAccount(AlipayConfirmRequestDto req, AlipayConfirmResponseDto res)
        {
            foreach (var alipayUser in res.List)
            {
                Current.AlipayUsers.Add(new AlipayUser
                {
                    UserId = alipayUser.AlipayUserId,
                    UserName = alipayUser.AlipayUserName,
                    LoginId = alipayUser.AlipayLoginId
                });
            }
        }
        private void RecalculateTotals()
        {
            decimal sumBuy = 0;
            decimal sumRefund = 0;

            foreach (var s in Current.SlipItems)
            {
                if (decimal.TryParse(s.TotalBuyAmt, out var buy))
                    sumBuy += buy;

                if (decimal.TryParse(s.TotalRefundAmt, out var refund))
                    sumRefund += refund;
            }

            Current.TotalBuyAmtSum = sumBuy;
            Current.TotalRefundAmtSum = sumRefund;
        }
    }
}
