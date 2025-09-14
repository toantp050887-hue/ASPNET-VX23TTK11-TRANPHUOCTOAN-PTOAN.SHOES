using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Models
{
	public class ProductsModels
	{
	}

    public class ProductLatestVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string MainImage { get; set; }
    }

    public class ProductListItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string MainImage { get; set; }
    }

    public class ProductsIndexVM
    {
        public IEnumerable<ProductListItemVM> Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Q { get; set; }
        public int? CategoryId { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
    }
    public class ProductDetailVM
    {
        // Core
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public double? Discount { get; set; }
        public decimal? FinalPrice { get; set; }
        public bool IsContact { get; set; }

        // Category for breadcrumb
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        // Media & Attributes
        public string MainImage { get; set; }
        public string[] Gallery { get; set; }
        public AttributeItem[] Attributes { get; set; }

        // Related
        public RelatedItem[] Related { get; set; }

        public class AttributeItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public class RelatedItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public decimal? Price { get; set; }
            public decimal? DiscountPrice { get; set; }
            public string MainImage { get; set; }
        }
    }


}