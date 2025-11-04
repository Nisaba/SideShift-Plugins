using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.SideShift.Models
{
    public class SideShiftRefund
    {
        public int OrderId { get; set; }
        public decimal FiatAmount { get; set; }
        public string FiatCurrency { get; set; }
        public decimal CryptoAmount { get; set; }
        public string CryptoCode { get; set; }
        public string CryptoNetwork { get; set; }
        public string Secret { get; set; }
    }
}
