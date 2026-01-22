using System.Collections.Generic;

namespace KIOSK.Presentation.Shared.Flow.ViewModels
{
    public sealed class FlowDefinition
    {
        public FlowDefinition(string serviceKey, IReadOnlyList<FlowStageDefinition> stages)
        {
            ServiceKey = serviceKey;
            Stages = stages;
        }

        public string ServiceKey { get; }
        public IReadOnlyList<FlowStageDefinition> Stages { get; }
    }

    public sealed class FlowStageDefinition
    {
        public FlowStageDefinition(FlowStage stage, string title)
        {
            Stage = stage;
            Title = title;
        }

        public FlowStage Stage { get; }
        public string Title { get; }
    }
}
