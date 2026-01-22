using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Features.ExchangeV2.ViewModels;
using KIOSK.Presentation.Shared.Flow.ViewModels;
using System.Collections.ObjectModel;

namespace KIOSK.Presentation.Features.ExchangeV2.Shell.ViewModels
{
    public sealed partial class ExchangeV2FlowHeaderViewModel : ObservableObject
    {
        [ObservableProperty]
        private FlowStage currentStage = FlowStage.Stage1;

        [ObservableProperty]
        private ObservableCollection<ExchangeV2FlowStageItem> stages = new();

        [ObservableProperty]
        private bool isVisible;

        public void EnsureDefinition()
        {
            if (Stages.Count > 0)
            {
                return;
            }

            Stages.Add(new ExchangeV2FlowStageItem(FlowStage.Stage1, "1", "언어 선택"));
            Stages.Add(new ExchangeV2FlowStageItem(FlowStage.Stage2, "2", "환전 유형"));
            Stages.Add(new ExchangeV2FlowStageItem(FlowStage.Stage3, "3", "수령 방식"));
            Stages.Add(new ExchangeV2FlowStageItem(FlowStage.Stage4, "4", "환전 통화"));
            Stages.Add(new ExchangeV2FlowStageItem(FlowStage.Stage5, "5", "확인"));
            UpdateStageFlags();
        }

        public void UpdateForView(object? view)
        {
            if (view is ExchangeV2ExchangeCurrencySelectViewModel)
            {
                EnsureDefinition();
                CurrentStage = FlowStage.Stage1;
                IsVisible = true;
                return;
            }

            IsVisible = false;
        }

        partial void OnCurrentStageChanged(FlowStage value)
        {
            UpdateStageFlags();
        }

        private void UpdateStageFlags()
        {
            foreach (var stage in Stages)
            {
                stage.IsCurrent = stage.Stage == CurrentStage;
            }
        }
    }

    public sealed partial class ExchangeV2FlowStageItem : ObservableObject
    {
        public ExchangeV2FlowStageItem(FlowStage stage, string number, string title)
        {
            Stage = stage;
            Number = number;
            Title = title;
        }

        public FlowStage Stage { get; }
        public string Number { get; }
        public string Title { get; }

        [ObservableProperty]
        private bool isCurrent;
    }
}
