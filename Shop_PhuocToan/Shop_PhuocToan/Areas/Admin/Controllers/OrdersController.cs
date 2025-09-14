using Shop_PhuocToan.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class OrdersController : Controller
    {
        // GET: Admin/Orders
        public ActionResult Index()
        {
            return View();
        }
    }
}