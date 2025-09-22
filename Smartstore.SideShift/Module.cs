global using System;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Smartstore.Web.Modelling;
using Smartstore.Engine.Modularity;
using Smartstore.SideShift.Settings;

namespace Smartstore.SideShift
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<SideShiftSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<SideShiftSettings>();

            await DeleteLanguageResourcesAsync();
            await DeleteLanguageResourcesAsync("Plugins.Payment.SideShift");

            await base.UninstallAsync();
        }
    }
}
