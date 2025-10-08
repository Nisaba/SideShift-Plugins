using Smartstore.Core.Configuration;

namespace Smartstore.SideShift.Settings
{
    public class SideShiftSettings : ISettings
    {
        /// <summary>
        /// Your SideShift API secret key used for authentication
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// The coin you want to receive (e.g., "BTC", "ETH")
        /// </summary>
        public string SettleCoin { get; set; }

        /// <summary>
        /// The network of the settle coin (e.g., "mainnet")
        /// </summary>
        public string SettleNetwork { get; set; }

        /// <summary>
        /// Required if the coin uses a memo/tag.
        /// </summary>
        public string? SettleMemo { get; set; }

        /// <summary>
        /// Your wallet address where the settlement will be sent.
        /// </summary>
        public string SettleAddress { get; set; }

        /// <summary>
        /// Number of decimals for the coin to be received.
        /// </summary>
        public ushort NbDecimalsCoin { get; set; }

        /// <summary>
        /// Flag if webhook is enabled, notifications for payment updates.
        /// </summary>
        public bool WebhookEnabled { get; set; }

        public string? WebhookId { get; set; }

        public decimal AdditionalFee { get; set; }

        public bool AdditionalFeePercentage { get; set; }

        public bool IsActive()
        {
            return !String.IsNullOrEmpty(PrivateKey) &&
                   !String.IsNullOrEmpty(SettleCoin) &&
                   !String.IsNullOrEmpty(SettleNetwork) &&
                   !String.IsNullOrEmpty(SettleAddress) &&  WebhookEnabled;
        }
    }
}
