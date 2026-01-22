using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Shared.Abstractions
{
    public interface IWindowHost
    {
        void SetShell(object? shell);
    }
}
