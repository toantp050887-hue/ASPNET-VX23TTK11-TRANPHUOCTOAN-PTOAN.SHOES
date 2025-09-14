using Shop_PhuocToan.DB;
using Shop_PhuocToan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Controllers
{
    public class ProductsController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();
        private readonly string MEMORY_PRODUCT_CACHE_KEY = "Shop_PhuocToan.Controllers.{0}";
        private readonly int MEMORY_PRODUCT_CACHE_TIME = 10;//10 phut
        private Dictionary<string, Tuple<DateTime, object>> MEMORY_PRODUCT_CACHE = new Dictionary<string, Tuple<DateTime, object>>();
        private readonly object _cacheLock = new object();


        private bool TryGetCache<T>(string key, out T value)
        {
            value = default(T);
            Tuple<DateTime, object> entry;

            lock (_cacheLock)
            {
                if (!MEMORY_PRODUCT_CACHE.TryGetValue(key, out entry))
                    return false;

                // Hết hạn thì xóa luôn
                if (entry.Item1 <= DateTime.UtcNow)
                {
                    MEMORY_PRODUCT_CACHE.Remove(key);
                    return false;
                }

                if (entry.Item2 is T casted)
                {
                    value = casted;
                    return true;
                }

                // Sai kiểu dữ liệu -> bỏ
                MEMORY_PRODUCT_CACHE.Remove(key);
                return false;
            }
        }

        private void SetCache(string key, object value, int minutes)
        {
            var expires = DateTime.UtcNow.AddMinutes(minutes);
            lock (_cacheLock)
            {
                MEMORY_PRODUCT_CACHE[key] = Tuple.Create(expires, value);
            }
        }

        /// <summary>
        /// Xóa toàn bộ cache trang Index (gọi sau Create/Update/Delete)
        /// </summary>
        private void ClearIndexCache()
        {
            lock (_cacheLock)
            {
                var prefix = string.Format(MEMORY_PRODUCT_CACHE_KEY, "Index");
                var keys = MEMORY_PRODUCT_CACHE.Keys.Where(k => k.StartsWith(prefix)).ToList();
                foreach (var k in keys) MEMORY_PRODUCT_CACHE.Remove(k);
            }
        }

        // GET: Products

        public ActionResult ClearCache()
        {
            ClearIndexCache();
            return Content("Đã xóa hết cache Index");
        }



        private string BuildIndexCacheKey(string q, int? categoryId, int page, int pageSize)
        {
            var key = $"Index:q={q?.Trim() ?? ""}|cat={(categoryId.HasValue ? categoryId.Value.ToString() : "")}|p={page}|ps={pageSize}";
            return string.Format(MEMORY_PRODUCT_CACHE_KEY, key);
        }
        public ActionResult Index(string q, int? categoryId, int page = 1, int pageSize = 24)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 24;

            var categories = db.Categories
                               .Where(c => c.IsActive)
                               .OrderBy(c => c.Name)
                               .Select(c => new { c.Id, c.Name })
                               .ToList()
                               .Select(c => new SelectListItem
                               {
                                   Value = c.Id.ToString(),
                                   Text = c.Name,
                                   Selected = (categoryId.HasValue && c.Id == categoryId.Value)
                               })
                               .ToList();

            if (!categories.Any(x => string.IsNullOrEmpty(x.Value)))
            {
                categories.Insert(0, new SelectListItem { Value = "", Text = "Tất cả danh mục", Selected = !categoryId.HasValue });
            }

            var query = db.Products.Include("Categories")
                          .Where(p => p.IsDeleted == null || p.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(p => p.Name.Contains(keyword) || p.ProductCode.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Categories.Id == categoryId.Value);
            }

            var cacheKey = BuildIndexCacheKey(q, categoryId, page, pageSize);
            var cacheKeyTotal = cacheKey + "|total";

            int total;
            if (!TryGetCache<int>(cacheKeyTotal, out total))// dùng cache để lưu total của danh sách
            {
                total = query.Count();
                SetCache(cacheKeyTotal, total, MEMORY_PRODUCT_CACHE_TIME);
            }

            List<ProductListItemVM> data;
            if (!TryGetCache<List<ProductListItemVM>>(cacheKey, out data))
            {
                data = query
                    .OrderByDescending(p => p.Created) // lưu danh sách
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductListItemVM
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        MainImage = p.ProductImages
                                      .Where(i => i.IsMain)
                                      .Select(i => i.ImageURL)
                                      .FirstOrDefault()
                    })
                    .ToList();

                SetCache(cacheKey, data, MEMORY_PRODUCT_CACHE_TIME);
            }


            var vm = new ProductsIndexVM
            {
                Items = data,
                Total = total,
                Page = page,
                PageSize = pageSize,
                Q = q,
                CategoryId = categoryId,
                Categories = categories
            };

            return View(vm);
        }


        [ChildActionOnly]
        public ActionResult Latest(int take = 12)
        {
            var products = db.Products
                             .Where(p => p.IsDeleted == null || p.IsDeleted == false)
                             .OrderByDescending(p => p.Created)
                             .Take(take)
                             .Select(p => new ProductLatestVM
                             {
                                 Id = p.Id,
                                 Name = p.Name,
                                 Slug = p.Slug,
                                 Price = p.Price,
                                 DiscountPrice = p.DiscountPrice,
                                 MainImage = p.ProductImages
                                              .Where(img => img.IsMain)
                                              .Select(img => img.ImageURL)
                                              .FirstOrDefault()
                             })
                             .ToList();

            return PartialView("_ProductLatest", products);
        }

        public ActionResult Details(int id)
        {
            var p = db.Products
                      .Where(x => x.Id == id && (x.IsDeleted == null || x.IsDeleted == false))
                      .FirstOrDefault();
            if (p == null) return HttpNotFound();


            var images = db.ProductImages.Include("Products")
                           .Where(i => i.Products.Id == p.Id)
                           .OrderByDescending(i => i.IsMain)
                           .ThenBy(i => i.Id)
                           .Select(i => i.ImageURL)
                           .ToList();


            var attrs = db.ProductAttributes
                          .Where(a => a.Id == p.Id && a.Deleted == null)
                          .OrderBy(a => a.AttributeName)
                          .Select(a => new ProductDetailVM.AttributeItem
                          {
                              Name = a.AttributeName,
                              Value = a.AttributeValue
                          }).ToArray();

            var cat = db.Categories.Where(c => c.Id == p.Categories.Id).Select(c => new { c.Id, c.Name, c.Slug }).FirstOrDefault();


            decimal? finalPrice = null;
            if (p.DiscountPrice.HasValue && p.DiscountPrice > 0)
                finalPrice = p.DiscountPrice;
            else if (p.Price.HasValue && p.Discount.HasValue)
                finalPrice = p.Price * (decimal)(1 - p.Discount);
            else
                finalPrice = p.Price;


            var related = db.Products
                            .Where(x => x.Id != p.Id
                                     && (x.IsDeleted == null || x.IsDeleted == false)
                                     && x.Categories.Id == p.Categories.Id)
                            .OrderByDescending(x => x.Created)
                            .Take(8)
                            .Select(x => new ProductDetailVM.RelatedItem
                            {
                                Id = x.Id,
                                Name = x.Name,
                                Slug = x.Slug,
                                Price = x.Price,
                                DiscountPrice = x.DiscountPrice,
                                MainImage = x.ProductImages.Where(i => i.IsMain).Select(i => i.ImageURL).FirstOrDefault()
                            })
                            .ToArray();

            var vm = new ProductDetailVM
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Discount = p.Discount,
                FinalPrice = finalPrice,
                IsContact = p.IsContact ?? false,

                CategoryId = p.Categories.Id,
                CategoryName = cat?.Name,

                MainImage = images.FirstOrDefault(),
                Gallery = images.ToArray(),
                Attributes = attrs,
                Related = related
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int qty = 1)
        {

            TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ.";
            return RedirectToAction("Details", new { id });
        }

    }

}
