using Autofac.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.SideShift.Models;
using Smartstore.SideShift.Providers;
using Smartstore.SideShift.Services;
using Smartstore.SideShift.Settings;
using Smartstore.Web.Controllers;

namespace Smartstore.SideShift.Controllers
{
    [Route("SideShift/Refund")]
    public class SideShiftRefundController(SmartDbContext db,
                                         ILogger logger,
                                         ICurrencyService currencyService,
                                         ICommonServices services,
                                         ISettingFactory settingFactory,
                                         IHttpContextAccessor httpContext) : PublicController
    {
        private readonly ILogger _logger = logger;
        private readonly SmartDbContext _db = db;
        private readonly ICommonServices _services = services;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly ISettingFactory _settingFactory = settingFactory;


        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int orderId, string secret, decimal amount)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(x =>
                x.Id == orderId &&
                x.PaymentMethodSystemName == PaymentProvider.SystemName &&
    //            x.PaymentStatus == PaymentStatus.Paid &&
                x.AuthorizationTransactionCode == secret);

            if (order == null)
                return NotFound();

            var myStore = _services.StoreContext.CurrentStore;
            var settings = await _settingFactory.LoadSettingsAsync<SideShiftSettings>(myStore.Id);
            var ip = GetClientIp();

            var sSwap = await SideShiftService.GetSwapInfo(order.AuthorizationTransactionResult, settings.PrivateKey, ip);
            dynamic JsonSwap = JsonConvert.DeserializeObject<dynamic>(sSwap);

            string sCoin = JsonSwap.depositCoin;
            sCoin = sCoin.Replace("0", "");

            var refund = new SideShiftRefund
            {
                OrderId = order.Id,
                FiatAmount = amount,
                CryptoAmount = await CryptoConverter.GetCorrespondingAmountAsync(amount, _currencyService.PrimaryCurrency.CurrencyCode, sCoin),
                CryptoCode = sCoin,
                CryptoNetwork = JsonSwap.depositNetwork,
                Secret = secret
            };

            return View(refund);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SideShiftRefund req)
        {
            try
            {
                var order = await _db.Orders.FirstOrDefaultAsync(x =>
                    x.Id == req.OrderId &&
                    x.PaymentMethodSystemName == PaymentProvider.SystemName &&
                    x.PaymentStatus == PaymentStatus.Paid &&
                    x.AuthorizationTransactionCode == req.Secret);
                if (order == null)
                    return NotFound();


                var provider = new PaymentProvider(_services, _settingFactory, _currencyService, _httpContext);
                var refundRequest = new RefundPaymentRequest
                {
                    Order = order,
                    AmountToRefund = new Core.Common.Money(req.FiatAmount, _currencyService.PrimaryCurrency)
                };
                var refundResult = await provider.RefundAsync(refundRequest);
                order.PaymentStatus = refundResult.NewPaymentStatus;
                await _db.SaveChangesAsync();
                ViewBag.Message = T("Plugins.SmartStore.SideShift.RefundSuccess").ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                ViewBag.Message = T("Plugins.SmartStore.SideShift.RefundError").ToString();
            }
            return View("Get", new { req.OrderId, req.Secret, req.FiatAmount });
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
