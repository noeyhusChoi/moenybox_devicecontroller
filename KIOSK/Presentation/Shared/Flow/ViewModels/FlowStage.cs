namespace KIOSK.Presentation.Shared.Flow.ViewModels
{
    public enum FlowStage
    {
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        Stage5
    }

    public interface IFlowStageProvider
    {
        FlowStage Stage { get; }
    }
}
