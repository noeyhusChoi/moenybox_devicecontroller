using System;
using System.Collections.Generic;

namespace KIOSK.Presentation.Shared.Flow.ViewModels
{
    public interface IFlowDefinitionProvider
    {
        FlowDefinition GetDefinition(string serviceKey);
    }

    public sealed class FlowDefinitionProvider : IFlowDefinitionProvider
    {
        private readonly Dictionary<string, FlowDefinition> _definitions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Service1",
                    new FlowDefinition(
                        "Service1",
                        new[]
                        {
                            new FlowStageDefinition(FlowStage.Stage1, "환전 정보"),
                            new FlowStageDefinition(FlowStage.Stage2, "수령 방식"),
                            new FlowStageDefinition(FlowStage.Stage3, "확인")
                        })
                },
                {
                    "Service2",
                    new FlowDefinition(
                        "Service2",
                        new[]
                        {
                            new FlowStageDefinition(FlowStage.Stage1, "시작"),
                            new FlowStageDefinition(FlowStage.Stage2, "계좌 입력"),
                            new FlowStageDefinition(FlowStage.Stage3, "영수증"),
                            new FlowStageDefinition(FlowStage.Stage4, "완료")
                        })
                },
                {
                    "Service3",
                    new FlowDefinition(
                        "Service3",
                        new[]
                        {
                            new FlowStageDefinition(FlowStage.Stage1, "약관 동의"),
                            new FlowStageDefinition(FlowStage.Stage2, "신분 확인")
                        })
                },
                {
                    "ExchangeV2",
                    new FlowDefinition(
                        "ExchangeV2",
                        new[]
                        {
                            new FlowStageDefinition(FlowStage.Stage1, "환전 통화")
                        })
                }
            };

        public FlowDefinition GetDefinition(string serviceKey)
        {
            if (_definitions.TryGetValue(serviceKey, out var definition))
                return definition;

            return _definitions["Service1"];
        }
    }
}
