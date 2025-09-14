using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Shop_PhuocToan.Areas.Admin.Models
{
    public class LoginViewModel
    {
        [Required, Display(Name = "Tài khoản")]
        public string UserName { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ")]
        public bool RememberMe { get; set; }
    }

}