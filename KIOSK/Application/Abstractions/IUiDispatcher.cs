using System;
using System.Threading.Tasks;

namespace KIOSK.Application.Abstractions
{
    public interface IUiDispatcher
    {
        Task InvokeAsync(Action action);
    }
}
