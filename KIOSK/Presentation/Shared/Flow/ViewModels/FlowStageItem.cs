using CommunityToolkit.Mvvm.ComponentModel;

namespace KIOSK.Presentation.Shared.Flow.ViewModels
{
    public sealed partial class FlowStageItem : ObservableObject
    {
        public FlowStageItem(FlowStage stage, string title)
        {
            Stage = stage;
            Title = title;
        }

        public FlowStage Stage { get; }
        public string Title { get; }

        [ObservableProperty]
        private bool isCurrent;
    }
}
