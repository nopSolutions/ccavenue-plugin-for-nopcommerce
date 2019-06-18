using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.CCAvenue
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //Return
            routeBuilder.MapRoute("Plugin.Payments.CCAvenue.Return", "Plugins/PaymentCCAvenue/Return",
                 new { controller = "PaymentCCAvenue", action = "Return" });
        }

        public int Priority => 0;
    }
}
