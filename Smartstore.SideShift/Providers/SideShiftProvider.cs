using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.SideShift.Components;
using Smartstore.SideShift.Controllers;
using Smartstore.SideShift.Models;
using Smartstore.SideShift.Services;
using Smartstore.SideShift.Settings;

namespace Smartstore.SideShift.Providers
{
    [SystemName("SmartStore.SideShift")]
    [FriendlyName("SideShift")]
    [Order(1)]
    public class PaymentProvider : PaymentMethodBase, IConfigurable
    {

        private readonly ICommonServices _services;
        private readonly ICurrencyService _currencyService;
        private readonly ISettingFactory _settingFactory;
        private readonly IHttpContextAccessor _httpContext;

        public PaymentProvider(
            ICommonServices services,
            ISettingFactory settingFactory,
            ICurrencyService currencyService,
            IHttpContextAccessor httpContext)
        {
            _services = services;
            _currencyService = currencyService;
            _settingFactory = settingFactory;
            _httpContext = httpContext;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public static string SystemName => "SmartStore.SideShift";
        public override bool SupportCapture => false;
        public override bool SupportPartiallyRefund => false;
        public override bool SupportRefund => false;
        public override bool SupportVoid => false;
        public override bool RequiresInteraction => false;
        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public RouteInfo GetConfigurationRoute()
            => new(nameof(SideShiftAdminController.Configure), "SideShift", new { area = "Admin" });


        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(SideShiftViewComponent));

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _settingFactory.LoadSettingsAsync<SideShiftSettings>(_services.StoreContext.CurrentStore.Id);
            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;

            try
            {

                var myStore = _services.StoreContext.CurrentStore;
                var settings = await _settingFactory.LoadSettingsAsync<SideShiftSettings>(myStore.Id);

                var ip = GetClientIp();
                var sCurrency = _currencyService.PrimaryCurrency.CurrencyCode ?? "USD";
                var cryptoAmount = await CryptoConverter.GetCryptoAmountAsync(processPaymentRequest.OrderTotal, sCurrency, settings.SettleCoin);
                var sUrl = myStore.Url.Replace("http://", "https://");
                var req = new SideShiftRequest()
                {
                    settleCoin = settings.SettleCoin,
                    settleNetwork = settings.SettleNetwork,
                    settleAmount = Math.Round(cryptoAmount, settings.NbDecimalsCoin, MidpointRounding.AwayFromZero),
                    settleAddress = settings.SettleAddress,
                    successUrl = sUrl + "checkout/completed",
                    cancelUrl = sUrl + "checkout",
                };
                if (!string.IsNullOrEmpty(settings.SettleMemo))
                {
                    req.settleMemo = settings.SettleMemo;
                }
                result.AuthorizationTransactionResult = await SideShiftService.CreateCheckout(req, settings.PrivateKey, ip);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw new PaymentException(ex.Message);
            }

            return await Task.FromResult(result);
        }

        public override Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest.Order.PaymentStatus == PaymentStatus.Pending)
            {
                // Specify redirection URL here if your provider is of type PaymentMethodType.Redirection.
                // Core redirects to this URL automatically.
                postProcessPaymentRequest.RedirectUrl = "https://pay.sideshift.ai/checkout/" + postProcessPaymentRequest.Order.AuthorizationTransactionResult;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// When true user can reprocess payment from MyAccount > Orders > OrderDetail
        /// </summary>
        public override Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.PaymentStatus == PaymentStatus.Pending && (DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds > 5)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private string GetClientIp()
        {
            var cntx = _httpContext.HttpContext;
            var ipAddress = cntx.Connection.RemoteIpAddress?.ToString();
            if (cntx.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = cntx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            else if (cntx.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = cntx.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            return ipAddress;
        }
    }
}
