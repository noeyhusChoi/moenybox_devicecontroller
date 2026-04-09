using Kiosk.Application.Services.API;
using Kiosk.Infrastructure.API.Cems;
using Kiosk.Infrastructure.API.Core;
using Kiosk.Infrastructure.API.Gtf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Kiosk.Infrastructure.Hosting.Modules
{
    public static class ApiModule
    {
        public static IServiceCollection AddApiModule(this IServiceCollection services)
        {
            services.AddHttpClient<IApiGateway, ApiGateway>((sp, http) =>
            {
                http.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddScoped<IApiClient, ApiClient>();

            services.AddOptions<CemsApiOptions>();
            services.AddSingleton<IConfigureOptions<CemsApiOptions>, CemsApiOptionsSetup>();
            services.AddScoped<ICemsApiCmdBuilder, CemsApiCmdBuilder>();
            services.AddScoped<CemsApiService>();

            services.AddOptions<GtfApiOptions>();
            services.AddSingleton<IConfigureOptions<GtfApiOptions>, GtfApiOptionsSetup>();
            services.AddScoped<IGtfApiCmdBuilder, GtfApiCmdBuilder>();
            services.AddScoped<GtfApiService>();

            return services;
        }
    }
}
