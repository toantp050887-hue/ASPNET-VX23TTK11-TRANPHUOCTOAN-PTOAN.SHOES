using Shop_PhuocToan.DB;
using Shop_PhuocToan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Controllers
{
    public class HomeController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();
  

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Index()
        {
            var allProducts = db.Products
                                .Where(p => p.IsDeleted == null || p.IsDeleted == false)
                                .OrderByDescending(p => p.Created)
                                .ToList();
            return View(allProducts);
        }
    }
}