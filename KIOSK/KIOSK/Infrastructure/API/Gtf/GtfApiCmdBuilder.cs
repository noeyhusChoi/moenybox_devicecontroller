using KIOSK.Infrastructure.API.Core;
using KIOSK.Infrastructure.API.Gtf;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace KIOSK.Infrastructure.API.Gtf
{
    public interface IGtfApiCmdBuilder
    {
        /*
            AP0001    initial             KIOSK 정보 확인
            AP0002    inquirySlipList     여권정보 입력
            AP0003    registerSlip        환급전표 스캔하여 등록
            AP0004    possibility         환급가능여부 요청
            AP0005    rollback            환급완료전 환급 가능여부 체크 후 에러 상황에 따른 원복
            AP0006    AlipayConfirm       ALIPAY 조회
            AP0007    AlipayRefund        ALIPAY 환급요청
            AP0008    availability        카드환급가능카드BIN체크
            AP0009    customsResult       관세청 시내환급 반출 요청
            AP0010    customsCancel       관세청에 시내환급 반출 취소 요청 후 결과전송
            AP0011    depositAmt          담보금 계산 후 결과전송
            AP0012    cardRefund          신용카드환급요청
            AP0013    saveMediSign        숙박 / 의료용역 전표 있는 경우 사인 이미지 저장
            AP0014    wechatRefund        위챗 환급 요청
         */

        ApiEnvelope Initial(InitialRequestDto dto);

        ApiEnvelope InquirySlipList(InquirySlipListRequestDto dto);
        ApiEnvelope RegisterSlip(RegisterSlipRequestDto dto);
        ApiEnvelope Possibility(PossibilityRequestDto dto);
        ApiEnvelope Rollback(RollbackRequestDto dto);

        ApiEnvelope AlipayConfirm(AlipayConfirmRequestDto dto);
        ApiEnvelope AlipayRefund(AlipayRefundRequestDto dto);

        ApiEnvelope Availability(AvailabilityRequestDto dto);
        ApiEnvelope DepositAmt(DepositAmtRequestDto dto);
        ApiEnvelope CardRefund(CardRefundRequestDto dto);
        ApiEnvelope SaveMediSign(SaveMediSignRequestDto dto);
        ApiEnvelope WechatRefund(WechatRefundRequestDto dto);

        ApiEnvelope CustomsResult(CustomsResultRequestDto dto);
        ApiEnvelope CustomsCancel(CustomsCancelRequestDto dto);
    }

    public sealed class GtfApiCmdBuilder : IGtfApiCmdBuilder
    {
        private readonly GtfApiOptions _opt;

        public GtfApiCmdBuilder(IOptions<GtfApiOptions> opt)
        {
            _opt = opt.Value;
        }

        private static string Idem(string apiName, string key)
            => $"{apiName}:{key}";

        private ApiEnvelope Env(string apiName, string route, object body, string idemKey = null)
        {
            var url = $"{_opt.BaseUrl}{route}";

            return new ApiEnvelope(
                Provider: "GTF",
                Method: HttpMethod.Post,
                RouteTemplate: url,     
                RouteValues: null,
                Query: null,
                Body: body,
                Headers: new Dictionary<string, string>(),
                IdempotencyKey: apiName // or null
            );
        }
        public ApiEnvelope Initial(InitialRequestDto dto)
            => Env("Initial", "/operation/initial", dto);

        public ApiEnvelope InquirySlipList(InquirySlipListRequestDto dto)
            => Env("InquirySlipList", "/trc/inquirySlipList", dto);

        public ApiEnvelope RegisterSlip(RegisterSlipRequestDto dto)
            => Env("RegisterSlip", "/trc/registerSlip", dto);

        public ApiEnvelope Possibility(PossibilityRequestDto dto)
            => Env("Possibility", "/trc/possibility", dto);

        public ApiEnvelope Rollback(RollbackRequestDto dto)
            => Env("Rollback", "/trc/rollback", dto);

        public ApiEnvelope AlipayConfirm(AlipayConfirmRequestDto dto)
            => Env("AlipayConfirm", "/refund/alipayConfirm", dto);

        public ApiEnvelope AlipayRefund(AlipayRefundRequestDto dto)
            => Env("AlipayRefund", "/refund/alipayRefund", dto);

        public ApiEnvelope Availability(AvailabilityRequestDto dto)
            => Env("Availability", "/refund/availability", dto);

        public ApiEnvelope DepositAmt(DepositAmtRequestDto dto)
            => Env("DepositAmt", "/refund/depositAmt", dto);

        public ApiEnvelope CardRefund(CardRefundRequestDto dto)
            => Env("CardRefund", "/refund/cardRefund", dto);

        public ApiEnvelope SaveMediSign(SaveMediSignRequestDto dto)
            => Env("SaveMediSign", "/refund/saveMediSign", dto);

        public ApiEnvelope WechatRefund(WechatRefundRequestDto dto)
            => Env("WechatRefund", "/refund/wechatRefund", dto);

        public ApiEnvelope CustomsResult(CustomsResultRequestDto dto)
            => Env("CustomsResult", "/trc/customsResult", dto);

        public ApiEnvelope CustomsCancel(CustomsCancelRequestDto dto)
            => Env("CustomsCancel", "/trc/customsCancel", dto);
    }
}
