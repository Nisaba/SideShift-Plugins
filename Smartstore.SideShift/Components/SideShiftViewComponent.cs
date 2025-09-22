using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.SideShift.Components
{
    public class SideShiftViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Modules/SmartStore.SideShift/Views/Public/PaymentInfo.cshtml");
        }
    }
}
