namespace KIOSK.Presentation.Shared.Abstractions
{
    // TODO: 글로벌, 로컬로 분리하여 계층 명확히 하기 (현재 모든 레이어가 IPopup 구현 중, 로컬에서 글로벌 접근 가능)
    public interface IPopup
    {
        /// <summary>
        /// 현재 표시 중인 Popup ViewModel (null = 팝업 없음)
        /// </summary>
        object? PopupContent { get; set; }
    }
}
