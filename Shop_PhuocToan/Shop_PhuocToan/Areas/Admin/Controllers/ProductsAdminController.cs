using Shop_PhuocToan.Areas.Admin.Models;
using Shop_PhuocToan.DB;
using Shop_PhuocToan.Infrastructure;
using Shop_PhuocToan.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop_PhuocToan.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class ProductsAdminController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();

        public ActionResult Index(string q, int? categoryId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

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
            categories.Insert(0, new SelectListItem { Value = "", Text = "Tất cả chủng loại", Selected = !categoryId.HasValue });

            var query = db.Products.Where(p => p.IsDeleted == null || p.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(p => p.Name.Contains(keyword) || p.ProductCode.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Categories.Id == categoryId.Value);
            }

            var total = query.Count();

            var data = query
                .OrderByDescending(p => p.Updated ?? p.Created)  
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductRowVM
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    Name = p.Name,
                    CategoryName = p.Categories.Id == null
                        ? "—"
                        : db.Categories.Where(c => c.Id == p.Categories.Id).Select(c => c.Name).FirstOrDefault(),
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    IsContact = p.IsContact,
                    Updated = p.Updated,
                    Created = p.Created,
                    MainImage = p.ProductImages.Where(i => i.IsMain).OrderByDescending(_=>_.Created).Select(i => i.ImageURL).FirstOrDefault()
                })
            .ToList();

            var vm = new ProductIndexVM
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

        [HttpGet]
        public ActionResult Create()
        {
            var vm = new ProductFormVM
            {
                Categories = GetCategorySelect(null),
                Statuses = GetStatusSelect(null),
                IsContact = false
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductFormVM vm)
        {
            vm.Categories = GetCategorySelect(vm.CategoryId);
            vm.Statuses = GetStatusSelect(vm.StatusId);

            if (!db.Categories.Any(c => c.Id == vm.CategoryId))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Chủng loại không hợp lệ.");
            }
            if (!db.ProductStatus.Any(s => s.Id == vm.StatusId))
            {
                ModelState.AddModelError(nameof(vm.StatusId), "Trạng thái sản phẩm không hợp lệ.");
            }
        

            if (!ModelState.IsValid) return View(vm);
            if (vm.Price.HasValue && vm.DiscountPrice.HasValue && vm.DiscountPrice > vm.Price)
            {
                ModelState.AddModelError(nameof(vm.DiscountPrice), "Giá khuyến mãi không được lớn hơn Giá.");
                return View(vm);
            }
            if (db.Products.Any(x => x.ProductCode == vm.ProductCode && (x.IsDeleted == null || x.IsDeleted == false)))
            {
                ModelState.AddModelError(nameof(vm.ProductCode), "Mã sản phẩm đã tồn tại.");
                return View(vm);
            }

            var p = new Products
            {
                ProductCode = vm.ProductCode.Trim(),
                Name = vm.Name.Trim(),
                Description = vm.Description,
                Price = vm.Price,
                DiscountPrice = vm.DiscountPrice,
                IsContact = vm.IsContact,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            p.Categories = GetCategoryOrThrow(vm.CategoryId);
            p.ProductStatus = GetStatusOrThrow(vm.StatusId);

            db.Products.Add(p);
            db.SaveChanges();

            SaveNewImages(vm, p.Id);           
            EnsureMainImage(p.Id, vm.MainImageId);
            return RedirectToAction("Index");
        }
        private Categories GetCategoryOrThrow(int id)
        {
            var cat = db.Categories.Local.FirstOrDefault(x => x.Id == id) ?? db.Categories.Find(id);
            if (cat == null)
                throw new InvalidOperationException($"Category Id={id} không tồn tại.");
            return cat;
        }

        private ProductStatus GetStatusOrThrow(int id)
        {
            var st = db.ProductStatus.Local.FirstOrDefault(x => x.Id == id) ?? db.ProductStatus.Find(id);
            if (st == null)
                throw new InvalidOperationException($"ProductStatus Id={id} không tồn tại.");
            return st;
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var p = db.Products.FirstOrDefault(x => x.Id == id && (x.IsDeleted == null || x.IsDeleted == false));
            if (p == null) return HttpNotFound();

            var vm = new ProductFormVM
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                CategoryId = p.Categories?.Id ?? 0,                      
                StatusId = p.ProductStatus?.Id ?? 0,                     
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                IsContact = p.IsContact?? false,
                Description = p.Description,
                Categories = GetCategorySelect(p.Categories?.Id),        
                Statuses = GetStatusSelect(p.ProductStatus?.Id),
                ExistingImages = p.ProductImages
                    .OrderByDescending(i => i.IsMain).ThenBy(i => i.Id)
                    .Select(i => new ProductImageItemVM { Id = i.Id, ImageURL = i.ImageURL, IsMain = i.IsMain })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductFormVM vm)
        {
            vm.Categories = GetCategorySelect(vm.CategoryId);
            vm.Statuses = GetStatusSelect(vm.StatusId);

            if (!db.Categories.Any(c => c.Id == vm.CategoryId))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Chủng loại không hợp lệ.");
            }
            if (!db.ProductStatus.Any(s => s.Id == vm.StatusId))
            {
                ModelState.AddModelError(nameof(vm.StatusId), "Trạng thái sản phẩm không hợp lệ.");
            }
            if (!ModelState.IsValid) return View(vm);

            if (vm.Price.HasValue && vm.DiscountPrice.HasValue && vm.DiscountPrice > vm.Price)
            {
                ModelState.AddModelError(nameof(vm.DiscountPrice), "Giá khuyến mãi không được lớn hơn Giá.");
                return View(vm);
            }

            var p = db.Products.FirstOrDefault(x => x.Id == vm.Id && (x.IsDeleted == null || x.IsDeleted == false));
            if (p == null) return HttpNotFound();

            if (db.Products.Any(x => x.ProductCode == vm.ProductCode && x.Id != p.Id && (x.IsDeleted == null || x.IsDeleted == false)))
            {
                ModelState.AddModelError(nameof(vm.ProductCode), "Mã sản phẩm đã tồn tại.");
                return View(vm);
            }

            p.ProductCode = vm.ProductCode.Trim();
            p.Name = vm.Name.Trim();
            p.Description = vm.Description;
            p.Price = vm.Price;
            p.DiscountPrice = vm.DiscountPrice;
            p.IsContact = vm.IsContact;
            p.Updated = DateTime.UtcNow;

            p.Categories = GetCategoryOrThrow(vm.CategoryId);
            p.ProductStatus = GetStatusOrThrow(vm.StatusId);

            db.SaveChanges();

            if (vm.DeleteImageIds != null && vm.DeleteImageIds.Length > 0)
            {
                var toDelete = p.ProductImages.Where(i => vm.DeleteImageIds.Contains(i.Id)).ToList();
                foreach (var img in toDelete)
                {
                    DeletePhysicalFile(img.ImageURL);
                    db.ProductImages.Remove(img);
                }
                db.SaveChanges();
            }

            SaveNewImages(vm, p.Id);        
            EnsureMainImage(p.Id, vm.MainImageId);

            return RedirectToAction("Index");
        }

        private SelectList GetCategorySelect(int? selectedId)
        {
            var list = db.Categories
                         .Where(c => c.IsActive)
                         .OrderBy(c => c.Name)
                         .Select(c => new { c.Id, c.Name })
                         .ToList();
            return new SelectList(list, "Id", "Name", selectedId);
        }

        private SelectList GetStatusSelect(int? selectedId)
        {
            var list = db.ProductStatus
                         .OrderBy(s => s.Id)
                         .Select(s => new { s.Id, s.Name })
                         .ToList();
            return new SelectList(list, "Id", "Name", selectedId);
        }

        private ProductFormVM BuildEmptyFormVM()
        {
            return new ProductFormVM
            {
                Categories = GetCategorySelect(null),
                IsContact = false
            };
        }

        private Categories AttachCategoryById(int id)
        {
            var stub = new Categories { Id = id };
            db.Categories.Attach(stub);   
            return stub;
        }

        private ProductStatus AttachStatusById(int id)
        {
            var stub = new ProductStatus { Id = id };
            db.ProductStatus.Attach(stub);
            return stub;
        }





        private void SaveNewImages(ProductFormVM vm, int productId)
        {
            if (vm.NewImages == null) return;

            var product = db.Products.Local.FirstOrDefault(x => x.Id == productId) ?? db.Products.Find(productId);
            if (product == null) throw new HttpException(404, "Product not found");

            var uploadRoot = Server.MapPath($"~/Uploads/Products/{productId}");
            if (!Directory.Exists(uploadRoot)) Directory.CreateDirectory(uploadRoot);

            foreach (var file in vm.NewImages)
            {
                if (file == null || file.ContentLength == 0) continue;

                var ext = Path.GetExtension(file.FileName);
                var fname = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadRoot, fname);
                file.SaveAs(savePath);

                var relUrl = $"/Uploads/Products/{productId}/{fname}";

                product.ProductImages.Add(new ProductImages
                {
                    ImageURL = relUrl,
                    IsMain = false,
                    Created = DateTime.UtcNow
                });
            }
            db.SaveChanges();
        }

        private void EnsureMainImage(int productId, int? selectedMainImageId)
        {
            var images = db.ProductImages.Include("Products").Where(i => i.Products.Id == productId).ToList(); 
            if (!images.Any()) return;

            foreach (var img in images) img.IsMain = false;
            db.SaveChanges();
            var target = selectedMainImageId.HasValue
                ? images.FirstOrDefault(i => i.Id == selectedMainImageId.Value)
                : null;

            (target ?? images.First()).IsMain = true;
            db.SaveChanges();
        }





        private void DeletePhysicalFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            var path = Server.MapPath("~" + relativeUrl.Replace("~", ""));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}