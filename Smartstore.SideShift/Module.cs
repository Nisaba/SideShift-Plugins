global using System;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Smartstore.Web.Modelling;
using Autofac.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core;
using Smartstore.Engine.Modularity;
using Smartstore.SideShift.Services;
using Smartstore.SideShift.Settings;

namespace Smartstore.SideShift
{
    internal class Module (ICommonServices services, ISettingFactory settingFactory) : ModuleBase
    {
        private readonly ICommonServices _services = services;
        private readonly ISettingFactory _settingFactory = settingFactory;

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<SideShiftSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            try
            {
                var myStore = _services.StoreContext.CurrentStore;
                var settings = await _settingFactory.LoadSettingsAsync<SideShiftSettings>(myStore.Id);
                if (settings.WebhookEnabled && !string.IsNullOrEmpty(settings.WebhookId) && !string.IsNullOrEmpty(settings.PrivateKey))
                    await SideShiftService.DeleteWebHook (settings.WebhookId, settings.PrivateKey);
            }
            catch { }

            await DeleteSettingsAsync<SideShiftSettings>();

            await DeleteLanguageResourcesAsync();
            await DeleteLanguageResourcesAsync("Plugins.Payment.SideShift");

            await base.UninstallAsync();
        }
    }
}
