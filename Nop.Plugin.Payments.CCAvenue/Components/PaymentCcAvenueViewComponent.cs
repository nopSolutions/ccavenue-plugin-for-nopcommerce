using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CCAvenue.Components
{
    [ViewComponent(Name = CCAvenueDefaults.VIEW_COMPONENT_NAME)]
    public class PaymentCcAvenueViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.CCAvenue/Views/PaymentInfo.cshtml");
        }
    }
}
