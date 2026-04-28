using Kiosk.Infrastructure.OCR;
using Kiosk.Infrastructure.OCR.Models;
using Kiosk.Infrastructure.OCR.Providers;
using Microsoft.Extensions.DependencyInjection;
using Pr22;

namespace Kiosk.Infrastructure.Hosting.Modules
{
    public static class OcrModule
    {
        public static IServiceCollection AddOcrModule(this IServiceCollection services)
        {
            services.AddSingleton<DocumentReaderDevice>();
            services.AddSingleton<OcrOptions>();
            services.AddSingleton<MrzOcrProvider>();
            services.AddSingleton<ExternalOcrProvider>();
            services.AddSingleton<IOcrService, OcrService>();
            return services;
        }
    }
}
