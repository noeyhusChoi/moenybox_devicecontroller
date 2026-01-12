using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public class ReceiptModel
    {
        public string Locale { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
