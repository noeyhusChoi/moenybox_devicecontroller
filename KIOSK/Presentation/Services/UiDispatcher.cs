using System;
using System.Threading.Tasks;
using KIOSK.Application.Abstractions;

namespace KIOSK.Presentation.Services
{
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        public Task InvokeAsync(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return dispatcher.InvokeAsync(action).Task;
        }
    }
}
