using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public class WithdrawalCassetteModel
    {
        public string DeviceID { get; set; }

        public string DeviceName { get; set; }
        
        public int Slot { get; set; }
        
        public string CurrencyCode { get; set; }
        
        public decimal Denomination { get; set; }
        
        public int Capacity { get; set; }
        
        public int Count { get; set; }
    }
}
