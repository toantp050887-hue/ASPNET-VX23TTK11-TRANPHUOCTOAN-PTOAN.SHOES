using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // Route mặc định cho khu vực Admin
            context.MapRoute(
                name: "Admin_default",
                url: "admin/{controller}/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Shop_PhuocToan.Areas.Admin.Controllers" }
            );
        }
    }

}