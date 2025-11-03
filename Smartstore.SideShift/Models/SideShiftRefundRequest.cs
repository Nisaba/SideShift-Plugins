using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.SideShift.Models
{
    public class SideShiftRefundRequest
    {
        public int OrderId { get; set; }
        public decimal CryptoAmount { get; set; }
        public string CryptoAddress { get; set; }
        public string CryptoCode { get; set; }
        public string CryptoNetwork { get; set; }
        public string Secret { get; set; }
    }
}
