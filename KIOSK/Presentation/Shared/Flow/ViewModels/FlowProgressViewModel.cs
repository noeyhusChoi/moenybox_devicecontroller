using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KIOSK.Presentation.Shared.Flow.ViewModels
{
    public sealed partial class FlowProgressViewModel : ObservableObject
    {
        [ObservableProperty]
        private FlowStage currentStage = FlowStage.Stage1;

        [ObservableProperty]
        private ObservableCollection<FlowStageItem> stages = new();

        [ObservableProperty]
        private bool isVisible;

        public void SetDefinition(FlowDefinition definition)
        {
            Stages.Clear();

            foreach (var stage in definition.Stages)
                Stages.Add(new FlowStageItem(stage.Stage, stage.Title));

            if (definition.Stages.Count > 0)
                CurrentStage = definition.Stages[0].Stage;

            UpdateStageFlags();
        }

        public void ClearDefinition()
        {
            Stages.Clear();
            CurrentStage = FlowStage.Stage1;
            IsVisible = false;
        }

        partial void OnCurrentStageChanged(FlowStage value)
        {
            UpdateStageFlags();
        }

        private void UpdateStageFlags()
        {
            foreach (var stage in Stages)
                stage.IsCurrent = stage.Stage == CurrentStage;
        }
    }
}
