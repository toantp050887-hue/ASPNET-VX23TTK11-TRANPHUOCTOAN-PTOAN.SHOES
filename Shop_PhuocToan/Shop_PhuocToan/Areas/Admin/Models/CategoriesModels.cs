using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Models
{
	public class CategoriesModels
	{
	}
    public class CategoryRowVM
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }

    public class CategoryIndexVM
    {
        public IEnumerable<CategoryRowVM> Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Q { get; set; }
        public int? ParentId { get; set; }
        public IEnumerable<SelectListItem> Parents { get; set; }
    }

    public class CategoryFormVM
    {
        public int? Id { get; set; }

        [StringLength(50)]
        public string Code { get; set; }

        [Required, StringLength(255)]
        public string Name { get; set; }

        public int? ParentId { get; set; }

        [StringLength(512)]
        public string Slug { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<SelectListItem> Parents { get; set; }
    }
}