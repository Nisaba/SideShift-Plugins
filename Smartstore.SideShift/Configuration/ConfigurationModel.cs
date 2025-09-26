namespace Smartstore.SideShift.Models
{
    [LocalizedDisplay("Plugins.Smartstore.SideShift.")]
    public class ConfigurationModel : ModelBase
    {

        [LocalizedDisplay("*PrivateKey")]
        [Required]
        public string PrivateKey { get; set; }

        [LocalizedDisplay("*SettleCoin")]
        [Required]
        public string SettleCoin { get; set; }

        [LocalizedDisplay("*SettleNetwork")]
        [Required]
        public string SettleNetwork { get; set; }

        [LocalizedDisplay("*SettleMemo")]
        public string? SettleMemo { get; set; }

        [LocalizedDisplay("*SettleAddress")]
        [Required]
        public string SettleAddress { get; set; }

        [LocalizedDisplay("*WebhookEnabled")]
        public bool WebhookEnabled { get; set; }


        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
    }


}