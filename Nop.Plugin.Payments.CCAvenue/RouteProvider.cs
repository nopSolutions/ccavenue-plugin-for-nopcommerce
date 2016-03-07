using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.CCAvenue
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Return
            routes.MapRoute("Plugin.Payments.CCAvenue.Return",
                 "Plugins/PaymentCCAvenue/Return",
                 new { controller = "PaymentCCAvenue", action = "Return" },
                 new[] { "Nop.Plugin.Payments.CCAvenue.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
