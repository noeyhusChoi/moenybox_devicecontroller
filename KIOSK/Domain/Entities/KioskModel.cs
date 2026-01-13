using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace KIOSK.Domain.Entities
{
    public class KioskModel
    {
        public string Id { get; set; } = string.Empty;

        public string Pid { get; set; } = string.Empty;

        public bool IsValid { get; set; }
    }
}
