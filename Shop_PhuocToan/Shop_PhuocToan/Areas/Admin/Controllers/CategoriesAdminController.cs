using Shop_PhuocToan.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shop_PhuocToan.Areas.Admin.Models;
using Shop_PhuocToan.Infrastructure;

namespace Shop_PhuocToan.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class CategoriesAdminController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();

        public ActionResult Index(string q, int? parentId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var parentOptions = db.Categories
                                  .OrderBy(c => c.Name)
                                  .Select(c => new { c.Id, c.Name })
                                  .ToList()
                                  .Select(c => new SelectListItem
                                  {
                                      Value = c.Id.ToString(),
                                      Text = c.Name,
                                      Selected = (parentId.HasValue && c.Id == parentId.Value)
                                  })
                                  .ToList();
            parentOptions.Insert(0, new SelectListItem { Value = "", Text = "— Tất cả —", Selected = !parentId.HasValue });

            var query = db.Categories.Include("Categories2").AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                query = query.Where(c => c.Name.Contains(key) || c.Code.Contains(key) || c.Slug.Contains(key));
            }
            if (parentId.HasValue)
            {
                query = query.Where(c => c.Categories2 != null && c.Categories2.Id == parentId.Value);
            }

            var total = query.Count();

            var rows = query
                .OrderByDescending(c => c.Updated ?? c.Created)
                .ThenBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryRowVM
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    ParentName = (c.Categories2 == null) ? null : c.Categories2.Name,
                    Slug = c.Slug,
                    IsActive = c.IsActive,
                    Created = c.Created,
                    Updated = c.Updated
                })
                .ToList();


            var vm = new CategoryIndexVM
            {
                Items = rows,
                Total = total,
                Page = page,
                PageSize = pageSize,
                Q = q,
                ParentId = parentId,
                Parents = parentOptions
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var vm = new CategoryFormVM
            {
                IsActive = true,
                Parents = BuildParentSelect(null, excludeIds: null)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryFormVM vm)
        {
            vm.Parents = BuildParentSelect(vm.ParentId, excludeIds: null);

            if (!ModelState.IsValid) return View(vm);

            if (!string.IsNullOrWhiteSpace(vm.Code))
            {
                var dupCode = db.Categories.Any(c => c.Code == vm.Code.Trim());
                if (dupCode)
                {
                    ModelState.AddModelError(nameof(vm.Code), "Mã đã tồn tại.");
                    return View(vm);
                }
            }

            var slug = string.IsNullOrWhiteSpace(vm.Slug)
                       ? Slugify(vm.Name)
                       : Slugify(vm.Slug);

            slug = EnsureUniqueSlug(slug, null);

            var entity = new Categories
            {
                Code = string.IsNullOrWhiteSpace(vm.Code) ? null : vm.Code.Trim(),
                Name = vm.Name.Trim(),
                
                Slug = slug,
                IsActive = vm.IsActive,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            db.Categories.Add(entity);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var c = db.Categories.Include("Categories2").FirstOrDefault(x => x.Id == id);
            if (c == null) return HttpNotFound();

            var descendants = GetDescendantIds(id);
            descendants.Add(id);

            var parentIdVal = (c.Categories2 == null) ? (int?)null : c.Categories2.Id;

            var vm = new CategoryFormVM
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                ParentId = parentIdVal,
                Slug = c.Slug,
                IsActive = c.IsActive,
                Parents = BuildParentSelect(parentIdVal, excludeIds: descendants)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CategoryFormVM vm)
        {
            var exclude = GetDescendantIds(vm.Id ?? 0);
            exclude.Add(vm.Id ?? 0);

            vm.Parents = BuildParentSelect(vm.ParentId, exclude);

            if (!ModelState.IsValid) return View(vm);

            var c = db.Categories.FirstOrDefault(x => x.Id == vm.Id);
            if (c == null) return HttpNotFound();

            if (vm.ParentId.HasValue && exclude.Contains(vm.ParentId.Value))
            {
                ModelState.AddModelError(nameof(vm.ParentId), "Không được chọn danh mục con/hậu duệ làm danh mục cha.");
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Code))
            {
                var dupCode = db.Categories.Any(x => x.Id != c.Id && x.Code == vm.Code.Trim());
                if (dupCode)
                {
                    ModelState.AddModelError(nameof(vm.Code), "Mã đã tồn tại.");
                    return View(vm);
                }
            }

            var newSlug = string.IsNullOrWhiteSpace(vm.Slug) ? Slugify(vm.Name) : Slugify(vm.Slug);
            newSlug = EnsureUniqueSlug(newSlug, c.Id);

            c.Code = string.IsNullOrWhiteSpace(vm.Code) ? null : vm.Code.Trim();
            c.Name = vm.Name.Trim();
            c.Slug = newSlug;
            c.IsActive = vm.IsActive;
            c.Updated = DateTime.UtcNow;

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ToggleActive(int id)
        {
            var c = db.Categories.FirstOrDefault(x => x.Id == id);
            if (c == null) return HttpNotFound();

            c.IsActive = !c.IsActive;
            c.Updated = DateTime.UtcNow;
            db.SaveChanges();

            return Json(new { ok = true, isActive = c.IsActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var c = db.Categories.FirstOrDefault(x => x.Id == id);
            if (c == null) return HttpNotFound();

            var hasChildren = db.Categories.Any(x => x.Categories2 != null && x.Categories2.Id == id);

            if (hasChildren)
            {
                TempData["Error"] = "Không thể xóa: Danh mục đang có danh mục con.";
                return RedirectToAction("Index");
            }

            db.Categories.Remove(c);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private SelectList BuildParentSelect(int? selectedId, ICollection<int> excludeIds)
        {
            var q = db.Categories.AsQueryable();
            if (excludeIds != null && excludeIds.Count > 0)
            {
                q = q.Where(c => !excludeIds.Contains(c.Id));
            }

            var items = q.OrderBy(c => c.Name)
                         .Select(c => new { c.Id, c.Name })
                         .ToList();

            var list = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "— Không chọn (cấp gốc) —", Selected = !selectedId.HasValue }
            };

            list.AddRange(items.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = (selectedId.HasValue && c.Id == selectedId.Value)
            }));

            return new SelectList(list, "Value", "Text", list.FirstOrDefault(x => x.Selected)?.Value);
        }

        private List<int> GetDescendantIds(int rootId)
        {
            var all = db.Categories
                .Select(c => new
                {
                    c.Id,
                    ParentId = (int?)(c.Categories2 == null ? (int?)null : c.Categories2.Id)
                })
                .ToList();

            var childrenByParent = all
                .Where(x => x.ParentId.HasValue)
                .GroupBy(x => x.ParentId.Value)
                .ToDictionary(g => g.Key, g => g.Select(i => i.Id).ToList());

            var result = new List<int>();
            var stack = new Stack<int>();

            if (childrenByParent.ContainsKey(rootId))
                foreach (var cid in childrenByParent[rootId]) stack.Push(cid);

            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!result.Contains(id))
                {
                    result.Add(id);
                    if (childrenByParent.ContainsKey(id))
                        foreach (var cid in childrenByParent[id]) stack.Push(cid);
                }
            }
            return result;
        }

        private string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var normalized = input.Trim().ToLowerInvariant();

            normalized = RemoveDiacritics(normalized);

            normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "-").Trim('-');

            normalized = Regex.Replace(normalized, @"-+", "-");

            return normalized;
        }

        private string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private string EnsureUniqueSlug(string baseSlug, int? currentId)
        {
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "category";
            var slug = baseSlug;
            int i = 2;
            while (db.Categories.Any(c => c.Slug == slug && (!currentId.HasValue || c.Id != currentId.Value)))
            {
                slug = $"{baseSlug}-{i++}";
            }
            return slug;
        }
    }
}