using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Status;

public interface IErrorMessageProvider
{
    string? GetMessage(ErrorCode code);
}

/// <summary>
/// 표준 코드 -> 메시지 매핑(ko).
/// </summary>
public sealed class StandardErrorMessageProvider : IErrorMessageProvider
{
    private static readonly IReadOnlyDictionary<string, string> Messages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // System
            ["SYS.APP.INIT.FAIL"] = "시스템 초기화에 실패했습니다.",
            ["SYS.APP.CONFIG.INVALID"] = "설정 값이 유효하지 않습니다.",
            ["SYS.APP.AUTH.INVALID_PIN"] = "비밀번호가 올바르지 않습니다.",
            ["SYS.APP.STATE.NOT_CONNECTED"] = "장치가 연결되지 않았습니다.",
            ["SYS.APP.INTERNAL.COMMAND"] = "명령 처리 중 오류가 발생했습니다.",
            ["SYS.APP.INTERNAL.FAULT"] = "시스템 오류가 발생했습니다.",

            // Printer
            ["DEV.PRINTER.CONNECT.FAIL"] = "프린터 연결에 실패했습니다.",
            ["DEV.PRINTER.STATUS.ERROR"] = "프린터 상태에 문제가 발생했습니다.",
            ["DEV.PRINTER.TIMEOUT.RESPONSE"] = "프린터 응답이 없습니다. 잠시 후 다시 시도해주세요.",
            ["DEV.PRINTER.STATUS.NO_PAPER"] = "영수증 용지가 없습니다.",
            ["DEV.PRINTER.STATUS.COVER_OPEN"] = "프린터 커버가 열려 있습니다.",
            ["DEV.PRINTER.STATUS.CUTTER"] = "프린터 커터에 문제가 발생했습니다.",
            ["DEV.PRINTER.STATUS.HEAD_UP"] = "프린터 헤드가 올라가 있습니다.",
            ["DEV.PRINTER.STATUS.PAPER_ERROR"] = "프린터 용지에 문제가 있습니다.",
            ["DEV.PRINTER.STATUS.PAPER_NEAR_END"] = "프린터 용지가 부족합니다.",
            ["DEV.PRINTER.STATUS.PRINTING"] = "프린터 출력 중입니다.",
            ["DEV.PRINTER.STATUS.AUX_PAPER_PRESENT"] = "보조 센서에 용지가 감지되었습니다.",

            // QR
            ["DEV.QR.CONNECT.FAIL"] = "QR 스캐너 연결에 실패했습니다.",
            ["DEV.QR.STATUS.ERROR"] = "QR 스캐너 상태에 문제가 발생했습니다.",
            ["DEV.QR.TIMEOUT.RESPONSE"] = "QR 스캐너 응답이 없습니다. 잠시 후 다시 시도해주세요.",
            ["DEV.QR.ERROR.DECODE"] = "QR 인식에 실패했습니다.",

            // ID Scanner
            ["DEV.IDSCANNER.CONNECT.FAIL"] = "신분증 스캐너 연결에 실패했습니다.",
            ["DEV.IDSCANNER.STATUS.ERROR"] = "신분증 스캐너 상태에 문제가 발생했습니다.",
            ["DEV.IDSCANNER.TIMEOUT.RESPONSE"] = "신분증 스캐너 응답이 없습니다. 잠시 후 다시 시도해주세요.",
            ["DEV.IDSCANNER.ERROR.SCAN"] = "신분증 스캔에 실패했습니다.",
            ["DEV.IDSCANNER.ERROR.IMAGE_SAVE"] = "신분증 이미지 저장에 실패했습니다.",

            // HCDM
            ["DEV.HCDM.CONNECT.FAIL"] = "지폐 방출기 연결에 실패했습니다.",
            ["DEV.HCDM.STATUS.ERROR"] = "지폐 방출기 상태에 문제가 발생했습니다.",
            ["DEV.HCDM.TIMEOUT.RESPONSE"] = "지폐 방출기 응답이 없습니다. 잠시 후 다시 시도해주세요.",
            ["DEV.HCDM.STATUS.SHUT_OPEN"] = "지폐 방출기 셔터가 열렸습니다.",
            ["DEV.HCDM.STATUS.SHUT_CLOSE"] = "지폐 방출기 셔터가 닫혔습니다.",
            ["DEV.HCDM.STATUS.SHUT_IN1"] = "지폐 방출기 셔터 IN1 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.SHUT_IN2"] = "지폐 방출기 셔터 IN2 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.SHUT_IN3"] = "지폐 방출기 셔터 IN3 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.MSOL_DISPENSE"] = "지폐 방출기 게이트가 방출 방향입니다.",
            ["DEV.HCDM.STATUS.MSOL_COLLECT"] = "지폐 방출기 게이트가 회수 방향입니다.",
            ["DEV.HCDM.STATUS.CIS_OPEN"] = "지폐 방출기 CIS가 열려 있습니다.",
            ["DEV.HCDM.STATUS.REJECT_BOX_UNLOCK"] = "지폐 방출기 리젝트 박스 잠금이 해제되었습니다.",
            ["DEV.HCDM.STATUS.SCAN_START"] = "지폐 방출기 스캔 타이밍 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.GATE1_DETECTED"] = "지폐 방출기 GATE1 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.GATE2_DETECTED"] = "지폐 방출기 GATE2 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.REJECT_IN_DETECTED"] = "지폐 방출기 REJECT IN 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.EXIT1_DETECTED"] = "지폐 방출기 EXIT1 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.CASSETTE_NOT_MOUNTED"] = "지폐 방출기 카세트가 미장착 상태입니다.",
            ["DEV.HCDM.STATUS.CASSETTE_NEAR_END"] = "지폐 방출기 카세트 시재가 부족합니다.",
            ["DEV.HCDM.STATUS.CASSETTE_SKEW1"] = "지폐 방출기 카세트 SKEW1 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.CASSETTE_SKEW2"] = "지폐 방출기 카세트 SKEW2 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.CASSETTE_ID1A"] = "지폐 방출기 카세트 ID1A 센서에 매체가 감지되었습니다.",
            ["DEV.HCDM.STATUS.CASSETTE_ID2A"] = "지폐 방출기 카세트 ID2A 센서에 매체가 감지되었습니다.",

            // Cash (Deposit)
            ["DEV.CASH.CONNECT.FAIL"] = "지폐 투입기 연결에 실패했습니다.",
            ["DEV.CASH.STATUS.ERROR"] = "지폐 투입기 상태에 문제가 발생했습니다.",
            ["DEV.CASH.TIMEOUT.RESPONSE"] = "지폐 투입기 응답이 없습니다. 잠시 후 다시 시도해주세요.",
            ["DEV.CASH.STATUS.ESCROW_DETECTED"] = "지폐 투입기 에스크로가 감지되었습니다.",
            ["DEV.CASH.STATUS.REJECT_DETECTED"] = "지폐 투입기에서 지폐가 리젝트되었습니다.",
            ["DEV.CASH.ERROR.STACK_FAIL"] = "지폐 투입기 스택에 실패했습니다.",
            ["DEV.CASH.ERROR.RETURN_FAIL"] = "지폐 투입기 반환에 실패했습니다.",

            // Network
            ["NET.DEVICE.CONNECT.FAIL"] = "네트워크 연결에 실패했습니다.",
            ["NET.DEVICE.TIMEOUT.RESPONSE"] = "장치 응답이 없습니다. 잠시 후 다시 시도해주세요."
        };

    public string? GetMessage(ErrorCode code)
        => Messages.TryGetValue(code.ToString(), out var msg) ? msg : null;
}
