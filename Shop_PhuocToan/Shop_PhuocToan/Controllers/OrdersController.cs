using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Shop_PhuocToan.DB;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace Shop_PhuocToan.Controllers
{
    public class OrdersController : Controller
    {
        private readonly Shop_PhuocToanEntities db = new Shop_PhuocToanEntities();
        private const string CART_KEY = "CART_ITEMS";

        // GET: /Orders
        public ActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Orders/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var product = db.Products
                            .Include(p => p.ProductImages)
                            .FirstOrDefault(p => p.Id == productId);

            if (product == null)
            {
                TempData["OrderError"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index");
            }

            var price = product.DiscountPrice ?? product.Price ?? product.OriginalPrice ?? 0M;
            var imageUrl = product.ProductImages != null && product.ProductImages.Any()
                ? (product.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageURL ?? product.ProductImages.First().ImageURL)
                : null;

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    ImageUrl = imageUrl,
                    Price = price,
                    Quantity = quantity
                });
            }
            SaveCart(cart);

            TempData["OrderInfo"] = "Đã thêm sản phẩm vào giỏ.";
            return RedirectToAction("Index");
        }

        // POST: /Orders/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1) quantity = 1;
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // POST: /Orders/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // POST: /Orders/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            SaveCart(new List<CartItem>());
            return RedirectToAction("Index");
        }

        // GET: /Orders/Purchase
        public ActionResult Purchase()
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                TempData["OrderError"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            ViewBag.Cart = cart;
            var vm = new PurchaseViewModel();
            return View(vm);
        }

        // POST: /Orders/Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Purchase(PurchaseViewModel vm)
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                return View(vm);
            }



            try
            {
                var order = new Orders
                {
                    OrderCode = GenerateOrderCode(),
                    CustomerName = vm.CustomerName,
                    Phone = vm.Phone,
                    Email = vm.Email,
                    Address = vm.Address,
                    PostCode = vm.PostCode,
                    Note = vm.Note,
                    Language = "vi",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    Status = 0
                };
                db.Orders.Add(order);
                db.SaveChanges();

                foreach (var item in cart)
                {
                    var product = db.Products.FirstOrDefault(_ => _.Id == item.ProductId);
                    db.OrderDetails.Add(new OrderDetails
                    {
                        Orders = order,
                        Products = product,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Discount = null
                    });
                }
                db.SaveChanges();


                SaveCart(new List<CartItem>());
                TempData["OrderSuccess"] = "Đặt hàng thành công! Mã đơn: " + order.OrderCode;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {

                ModelState.AddModelError("", "Có lỗi khi lưu đơn hàng: " + ex.Message);
                ViewBag.Cart = cart;
                return View(vm);
            }

        }

        // Helpers
        private List<CartItem> GetCart()
        {
            var cart = Session[CART_KEY] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session[CART_KEY] = cart;
            }
            return cart;
        }

        private void SaveCart(List<CartItem> cart)
        {
            Session[CART_KEY] = cart;
        }

        private string GenerateOrderCode()
        {
            // Đảm bảo tính duy nhất theo thời gian; có thể bổ sung random nếu cần
            return "OD" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        }
    }

    // ===== ViewModels / DTOs =====
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get { return Price * Quantity; } }
    }

    public class PurchaseViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Mã bưu chính")]
        public string PostCode { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }
    }
}
