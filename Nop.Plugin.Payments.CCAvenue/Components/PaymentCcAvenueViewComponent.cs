using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CCAvenue.Components
{
    [ViewComponent(Name = "PaymentCCAvenue")]
    public class PaymentCcAvenueViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.CCAvenue/Views/PaymentInfo.cshtml");
        }
    }
}
