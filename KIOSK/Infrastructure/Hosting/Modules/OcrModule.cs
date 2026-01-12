using KIOSK.Infrastructure.OCR;
using KIOSK.Infrastructure.OCR.Models;
using KIOSK.Infrastructure.OCR.Providers;
using Microsoft.Extensions.DependencyInjection;
using Pr22;

namespace KIOSK.Infrastructure.Hosting.Modules
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
