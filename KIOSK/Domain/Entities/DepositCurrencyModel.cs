using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public class DepositCurrencyModel
    {
        public string CurrencyCode{get; set;} = string.Empty;
        
        public decimal Denomination { get; set;}

        public string AttributeCode { get; set; } = string.Empty;
    }
}
