

namespace Smartstore.SideShift.Models
{
    public class SideShiftRequest
    {
        public string settleCoin { get; set; }
        public string settleNetwork { get; set; }
        public decimal settleAmount { get; set; }
        public string settleAddress { get; set; }
        public string? settleMemo { get; set; }
        public string affiliateId { get; set; }
        public string successUrl { get; set; }
        public string cancelUrl { get; set; }
    }
}
