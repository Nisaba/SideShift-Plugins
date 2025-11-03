using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.SideShift.Services;
using Smartstore.SideShift.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.SideShift.Models;
using Smartstore.Core.Checkout.Payment;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Smartstore.SideShift.Controllers
{
    [Area("Admin")]
    [Route("[area]/sideshift/{action=index}/{id?}")]
    public class SideShiftAdminController (ICommonServices services, IProviderManager providerManager, ICurrencyService currencyService, PaymentSettings paymentSettings) : ModuleController
    {

        private readonly IProviderManager _providerManager = providerManager;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly ICommonServices _services = services;
        private readonly PaymentSettings _paymentSettings = paymentSettings;


        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(int storeId, SideShiftSettings settings)
        {
            var model = MiniMapper.Map<SideShiftSettings, ConfigurationModel>(settings);
            ViewBag.Provider = _providerManager.GetProvider("Smartstore.SideShift").Metadata;
            ViewBag.StoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode ?? "USD";
            var sViewMsgError = HttpContext.Session.GetString("ViewMsgError");
            if (!string.IsNullOrEmpty(sViewMsgError))
            {
                ViewBag.ViewMsgError = sViewMsgError;
                HttpContext.Session.SetString("ViewMsgError", "");
            }
            var sViewMsg = HttpContext.Session.GetString("ViewMsg");
            if (!string.IsNullOrEmpty(sViewMsg))
            {
                ViewBag.ViewMsg = sViewMsg;
                HttpContext.Session.SetString("ViewMsg", "");
            }

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public async Task<IActionResult> Configure(int storeId, ConfigurationModel model, SideShiftSettings settings, string command = null)
        {
            if (command == "delete")
            {
                try
                {
                    if (settings.WebhookEnabled && !string.IsNullOrEmpty(settings.WebhookId) && !string.IsNullOrEmpty(settings.PrivateKey))
                        await SideShiftService.DeleteWebHook(settings.WebhookId, settings.PrivateKey);
                }
                catch { }

                settings.SettleMemo = "";
                settings.SettleCoin = "";
                settings.NbDecimalsCoin = 8;
                settings.PrivateKey = "";
                settings.SettleAddress = "";
                settings.SettleNetwork = "";
                settings.WebhookEnabled = false;
                settings.WebhookId = "";
                settings.AdditionalFee = 0;
                settings.AdditionalFeePercentage = false;

                ModelState.Clear();
                _paymentSettings.ActivePaymentMethodSystemNames.Remove("Smartstore.SideShift");

                //HttpContext.Session.SetString("ViewMsg", "Settings cleared and payment method deactivated");
                HttpContext.Session.SetString("ViewMsg", "Settings cleared");
                return Configure(storeId, settings);
            }

            if (command == "activate" && model.IsConfigured())
            {
                _paymentSettings.ActivePaymentMethodSystemNames.Add("Smartstore.SideShift");

                await _services.SettingFactory.SaveSettingsAsync(_paymentSettings);

                HttpContext.Session.SetString("ViewMsg", "Payment method activated");
                return Configure(storeId, settings);
            }

            if (!ModelState.IsValid)
            {
                HttpContext.Session.SetString("ViewMsgError", "Incorrect data");
                return Configure(storeId, settings);
            }

            if (!model.WebhookEnabled)
            {
                var myStore = _services.StoreContext.CurrentStore;

                try
                {
                    var sUrl = myStore.Url.Replace("http://", "https://") + "SideShift/Hook";
                    var t = await SideShiftService.InitWebHook(sUrl, model.PrivateKey);
                    model.WebhookEnabled = t.Item1;
                    model.WebhookId = t.Item2;
                }
                catch (Exception ex)
                {
                    HttpContext.Session.SetString("ViewMsgError", "Error during SideShift webhook creation: " + ex.Message);
                }
            }

           /* if (!model.WebhookEnabled)
            {
                HttpContext.Session.SetString("ViewMsgError", "Error during SideShift webhook creation");
                return Configure(storeId, settings);
            }*/

            ModelState.Clear();
            MiniMapper.Map(model, settings);
            try
            {
                settings.NbDecimalsCoin = await SideShiftService.CheckCoin(model.SettleCoin, model.SettleNetwork, model.SettleMemo);
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("ViewMsgError", "Error during SideShift coin/network check: " + ex.Message);
                return Configure(storeId, settings);
            }

            HttpContext.Session.SetString("ViewMsg", "Save OK");
            //            return RedirectToAction(nameof(Configure));
            return Configure(storeId, settings);
        }

    }
}