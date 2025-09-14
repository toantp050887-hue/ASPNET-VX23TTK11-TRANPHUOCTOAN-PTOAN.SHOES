using Shop_PhuocToan.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Admin = Session["AdminFullName"] ?? Session["AdminUserName"];
            return View();
        }
    }
}