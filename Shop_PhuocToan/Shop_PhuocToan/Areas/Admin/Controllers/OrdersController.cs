using Shop_PhuocToan.DB;
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
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();

        // GET: /Admin/Orders
        // Bộ lọc: keyword (OrderCode/Name/Phone/Email), status, date range
        public ActionResult Index(string q, int? status, DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            // NOTE: Project uses EDMX/ObjectContext (MVC4). Avoid nested DbQuery usage inside projections.
            var query = db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(o =>
                    o.OrderCode.Contains(q) ||
                    o.CustomerName.Contains(q) ||
                    o.Phone.Contains(q) ||
                    o.Email.Contains(q));
            }

            if (status.HasValue)
                query = query.Where(o => o.Status == status);

            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(o => o.CreateDate >= f);
            }

            if (to.HasValue)
            {
                var t = to.Value.Date.AddDays(1);
                query = query.Where(o => o.CreateDate < t);
            }

            var totalRows = query.Count();

            // STEP 1: page orders (materialize)
            var ordersPage = query
                .OrderByDescending(o => o.CreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var orderIds = ordersPage.Select(o => o.Id).ToList();

            // STEP 2: compute totals in one grouped query, then join in-memory
            var totals = db.OrderDetails.Include("Orders")
                           .Where(d => orderIds.Contains(d.Orders.Id))
                           .GroupBy(d => d.Orders.Id)
                           .Select(g => new
                           {
                               OrderId = g.Key,
                               Total = (decimal?)g.Sum(x => x.Price * x.Quantity) ?? 0M
                           })
                           .ToList();

            var data = ordersPage
                .Select(o => new AdminOrderListItem
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    Phone = o.Phone,
                    Email = o.Email,
                    CreateDate = o.CreateDate,
                    Status = o.Status,
                    Total = totals.FirstOrDefault(t => t.OrderId == o.Id)?.Total ?? 0M
                })
                .ToList();

            var vm = new AdminOrderIndexViewModel
            {
                Items = data,
                Q = q,
                Status = status,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            };

            ViewBag.StatusList = GetStatusSelectList(status);
            return View(vm);
        }

        // GET: /Admin/Orders/Details/5
        public ActionResult Details(int id)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            // Lấy chi tiết + tên sản phẩm
            var details = (from d in db.OrderDetails.Include("Products").Include("Orders")
                           join p in db.Products on d.Products.Id equals p.Id
                           where d.Orders.Id == order.Id
                           select new AdminOrderDetailItem
                           {
                               ProductId = p.Id,
                               ProductName = p.Name,
                               Quantity = d.Quantity,
                               Price = d.Price,
                               Discount = d.Discount
                           }).ToList();

            var vm = new AdminOrderDetailsViewModel
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                CustomerName = order.CustomerName,
                Phone = order.Phone,
                Email = order.Email,
                Address = order.Address,
                PostCode = order.PostCode,
                Note = order.Note,
                Language = order.Language,
                CreateDate = order.CreateDate,
                UpdateDate = order.UpdateDate,
                Status = order.Status,
                Lines = details,
                Total = details.Sum(x => x.LineTotal)
            };

            ViewBag.StatusList = GetStatusSelectList(order.Status);
            return View(vm);
        }

        // POST: /Admin/Orders/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeStatus(int id, int status)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            order.Status = status;
            order.UpdateDate = DateTime.Now;
            db.SaveChanges();

            TempData["AdminInfo"] = "Đã cập nhật trạng thái đơn.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Admin/Orders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            // Cảnh báo: Xóa cứng đơn sẽ cascade xóa OrderDetails theo FK
            db.Orders.Remove(order);
            db.SaveChanges();

            TempData["AdminInfo"] = "Đã xóa đơn hàng.";
            return RedirectToAction("Index");
        }

        // ===== Helpers =====
        private List<SelectListItem> GetStatusSelectList(int? selected)
        {
            // Map trạng thái cho Orders.Status (tùy hệ thống, bạn chỉnh lại label)
            var pairs = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(0, "Mới tạo"),
                new KeyValuePair<int, string>(1, "Đã xác nhận"),
                new KeyValuePair<int, string>(2, "Đang giao"),
                new KeyValuePair<int, string>(3, "Hoàn tất"),
                new KeyValuePair<int, string>(4, "Hủy")
            };

            return pairs.Select(p => new SelectListItem
            {
                Text = p.Value,
                Value = p.Key.ToString(),
                Selected = selected.HasValue && selected.Value == p.Key
            }).ToList();
        }
    }

    // ===== ViewModels =====
    public class AdminOrderIndexViewModel
    {
        public List<AdminOrderListItem> Items { get; set; }
        public string Q { get; set; }
        public int? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRows { get; set; }
    }

    public class AdminOrderListItem
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? Status { get; set; }
        public decimal Total { get; set; }
    }

    public class AdminOrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string Note { get; set; }
        public string Language { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? Status { get; set; }
        public List<AdminOrderDetailItem> Lines { get; set; }
        public decimal Total { get; set; }
    }

    public class AdminOrderDetailItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public double? Discount { get; set; }
        public decimal LineTotal { get { return Price * Quantity; } }
    }
}