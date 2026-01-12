using KIOSK.Application.StateMachines;
using KIOSK.Presentation.Features.Exchange.Flow;
using KIOSK.Presentation.Features.GTF.Flow;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class StateMachineModule
    {
        public static IServiceCollection AddStateMachineModule(this IServiceCollection services)
        {
            services.AddScoped<ExchangeSellStateMachine>();
            services.AddScoped<GtfStateMachine>();
            services.AddScoped<ExchangeFlowCoordinator>();
            services.AddScoped<GtfFlowCoordinator>();
            return services;
        }
    }
}
