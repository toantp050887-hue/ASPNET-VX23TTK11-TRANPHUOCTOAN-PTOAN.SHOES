using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Shop_PhuocToan
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            var route = routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Shop_PhuocToan.Controllers" }   // <-- khóa namespace frontend
            );
            // NGĂN fallback sang namespace khác (vd: Areas.Admin)
            route.DataTokens = route.DataTokens ?? new RouteValueDictionary();
            route.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
