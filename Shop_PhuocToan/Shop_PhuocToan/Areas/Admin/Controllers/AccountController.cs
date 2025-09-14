using Shop_PhuocToan.Areas.Admin.Models;
using Shop_PhuocToan.DB;
using Shop_PhuocToan.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm, string returnUrl)
        {
            if (!ModelState.IsValid) return View(vm);

            var username = vm.UserName.Trim();
            var inputHash = PasswordHasher.Sha256(vm.Password);

            var user = db.Staff
                         .Where(s => s.IsActive == true
                                  && s.UserName == username
                                  && (s.Password == inputHash || s.Password == vm.Password)) 
                         .Select(s => new {
                             s.Id,
                             s.FullName,
                             s.UserName,
                             s.Type,
                             s.PermissionType
                         })
                         .FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu, hoặc tài khoản bị khóa.");
                return View(vm);
            }

            var isAdmin = (user.Type >= 1) ||
                          (!string.IsNullOrEmpty(user.PermissionType) && user.PermissionType.ToUpper().Contains("ADMIN"));
            if (!isAdmin)
            {
                ModelState.AddModelError("", "Bạn không có quyền truy cập khu vực quản trị.");
                return View(vm);
            }

            Session["AdminUserId"] = user.Id;
            Session["AdminUserName"] = user.UserName;
            Session["AdminFullName"] = user.FullName;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Remove("AdminUserId");
            Session.Remove("AdminUserName");
            Session.Remove("AdminFullName");
            Session.Clear();
            return RedirectToAction("Login");
        }
    }

}