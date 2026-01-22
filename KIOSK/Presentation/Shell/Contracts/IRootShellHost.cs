using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Shell.Contracts
{
    public interface IRootShellHost
    {
        void SetShell(object? shell);
    }
}
