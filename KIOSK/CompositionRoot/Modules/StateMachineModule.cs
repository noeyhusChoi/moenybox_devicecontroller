using KIOSK.Application.StateMachines;
using KIOSK.Presentation.Features.Exchange.Flow;
using KIOSK.Presentation.Features.ExchangeV2.Flow;
using KIOSK.Presentation.Features.GTF.Flow;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class StateMachineModule
    {
        public static IServiceCollection AddStateMachineModule(this IServiceCollection services)
        {
            services.AddScoped<ExchangeSellStateMachine>();
            services.AddScoped<ExchangeV2StateMachine>();
            services.AddScoped<GtfStateMachine>();
            services.AddScoped<ExchangeFlowCoordinator>();
            services.AddScoped<ExchangeV2FlowCoordinator>();
            services.AddScoped<GtfFlowCoordinator>();
            return services;
        }
    }
}
