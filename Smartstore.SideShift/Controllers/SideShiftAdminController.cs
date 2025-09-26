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

namespace Smartstore.SideShift.Controllers
{
    [Area("Admin")]
    [Route("[area]/sideshift/{action=index}/{id?}")]
    public class SideShiftAdminController (ICommonServices services, IProviderManager providerManager, ICurrencyService currencyService) : ModuleController
    {

        private readonly IProviderManager _providerManager = providerManager;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly ICommonServices _services = services;


        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(int storeId, SideShiftSettings settings)
        {
            var model = MiniMapper.Map<SideShiftSettings, ConfigurationModel>(settings);
            ViewBag.Provider = _providerManager.GetProvider("SmartStore.SideShift").Metadata;
            ViewBag.StoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode ?? "USD";
            var sViewMsgError = HttpContext.Session.GetString("ViewMsgError");
            if (!string.IsNullOrEmpty(sViewMsgError))
            {
                ViewBag.ViewMsgError = sViewMsgError;
                HttpContext.Session.SetString("ViewMsgError", "");
            }

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public async Task<IActionResult> Configure(int storeId, ConfigurationModel model, SideShiftSettings settings)
        {
            if (!ModelState.IsValid)
            {
                HttpContext.Session.SetString("ViewMsgError", "Incorrect data");
                return Configure(storeId, settings);
            }

            if (!model.WebhookEnabled)
            {
                var myStore = _services.StoreContext.CurrentStore;

                var srv = new SideShiftService();
                try
                {
                    model.WebhookEnabled = await srv.InitWebHook(myStore.Url + "SideShiftHook/Process", model.PrivateKey);
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

            HttpContext.Session.SetString("ViewMsg", "Save OK");
            return RedirectToAction(nameof(Configure));
        }

    }
}