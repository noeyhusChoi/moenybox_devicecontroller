using KIOSK.Application.Services;
using KIOSK.Application.Services.BackgroundTasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class BackgroundModule
    {
        public static IServiceCollection AddBackgroundModule(this IServiceCollection services)
        {
            services.AddHostedService<BackgroundTaskService>();
            services.AddSingleton<SendCemsTxResultTask>();
            services.AddSingleton<UpdateExchangeRateTask>();

            services.AddSingleton(new BackgroundTaskDescriptor(
                name: "SENT_CEMS_TX_RESULT",
                interval: TimeSpan.FromSeconds(30),
                action: async (sp, ct) =>
                {
                    var handler = sp.GetRequiredService<SendCemsTxResultTask>();
                    await handler.ExecuteAsync(ct);
                }));

            services.AddSingleton(new BackgroundTaskDescriptor(
                name: "UPDATE_EXCHANGE_RATE",
                interval: TimeSpan.FromSeconds(30),
                action: async (sp, ct) =>
                {
                    var handler = sp.GetRequiredService<UpdateExchangeRateTask>();
                    await handler.ExecuteAsync(ct);
                }));

            return services;
        }
    }
}
