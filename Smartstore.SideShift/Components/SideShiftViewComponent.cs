using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.SideShift.Components
{
    public class SideShiftViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Modules/Smartstore.SideShift/Views/Public/PaymentInfo.cshtml");
        }
    }
}
