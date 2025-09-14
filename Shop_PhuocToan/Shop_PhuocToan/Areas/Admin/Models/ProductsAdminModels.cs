using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Models
{
	public class ProductsAdminModels
	{
	}
    public class ProductRowVM
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public bool? IsContact { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime? Created { get; set; }
        public string MainImage { get; set; }
    }

    public class ProductIndexVM
    {
        public IEnumerable<ProductRowVM> Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Q { get; set; }
        public int? CategoryId { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
    }

    public class ProductImageItemVM
    {
        public int Id { get; set; }
        public string ImageURL { get; set; }
        public bool IsMain { get; set; }
    }

    public class ProductFormVM
    {
        public int? Id { get; set; }

        public string ProductCode { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }       

        public int StatusId { get; set; }        

        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public bool IsContact { get; set; }

        public string Description { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Statuses { get; set; }

        public List<ProductImageItemVM> ExistingImages { get; set; }
        public IEnumerable<HttpPostedFileBase> NewImages { get; set; }
        public int? MainImageId { get; set; }
        public int[] DeleteImageIds { get; set; } = Array.Empty<int>();
    }

}