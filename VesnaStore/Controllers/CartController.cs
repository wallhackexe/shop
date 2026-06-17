using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VesnaStore.Data;
using VesnaStore.Models;

namespace VesnaStore.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. ПРОСМОТР КОРЗИНЫ ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart" });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdString);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            var invalidItems = cartItems
                .Where(c => c.Product == null || !c.Product.IsPublished || c.Product.StockQuantity <= 0)
                .ToList();

            if (invalidItems.Any())
            {
                _context.CartItems.RemoveRange(invalidItems);
                await _context.SaveChangesAsync();

                TempData["Error"] = "Некоторые товары были удалены из корзины, так как они закончились или сняты с продажи.";

                cartItems = cartItems.Except(invalidItems).ToList();
            }

            return View(cartItems);
        }

        [HttpGet]
        public async Task<int> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated) return 0;

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            return await _context.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);
        }

        // --- 2. ДОБАВЛЕНИЕ ТОВАРА (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Пожалуйста, войдите в аккаунт." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Товар не найден." });

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId);

            int currentQtyInCart = existingItem != null ? existingItem.Quantity : 0;

            if (currentQtyInCart + 1 > product.StockQuantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"На складе доступно только {product.StockQuantity} шт."
                });
            }

            if (existingItem == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    UserID = userId,
                    ProductID = productId,
                    Quantity = 1
                });
            }
            else
            {
                existingItem.Quantity += 1;
            }

            await _context.SaveChangesAsync();

            int cartCount = await _context.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        // --- 3. УДАЛЕНИЕ ТОВАРА ---
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- 4. ОБНОВЛЕНИЕ КОЛИЧЕСТВА (+ и - в корзине) ---
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int delta)
        {
            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartItemID == cartItemId);

            if (item != null)
            {
                int newQuantity = item.Quantity + delta;

                if (newQuantity > item.Product.StockQuantity)
                {
                    TempData["Error"] = $"Извините, в наличии только {item.Product.StockQuantity} шт.";
                    return RedirectToAction(nameof(Index));
                }

                item.Quantity = newQuantity;

                if (item.Quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DecreaseOne(int productId)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Войдите в аккаунт." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId);

            if (item != null)
            {
                item.Quantity -= 1;

                if (item.Quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                await _context.SaveChangesAsync();
            }

            int cartCount = await _context.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        // --- 5. ПРОМОКОДЫ ---
        public class PromoApplyModel
        {
            public string Code { get; set; }
            public decimal CurrentTotal { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPromo([FromBody] PromoApplyModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, message = "Промокод не введен!" });

            string cleanCode = request.Code.Trim().ToUpper();

            var promoCode = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == cleanCode && p.IsActive);

            if (promoCode == null)
            {
                return Json(new { success = false, message = "Такого промокода не существует или он неактивен." });
            }

            if (promoCode.UsedCount >= promoCode.MaxUses)
            {
                return Json(new { success = false, message = "Этот промокод уже закончился." });
            }

            if (request.CurrentTotal < promoCode.MinOrderAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Этот промокод работает только для заказов от {promoCode.MinOrderAmount:N0} ₽"
                });
            }

            return Json(new
            {
                success = true,
                message = $"Промокод {promoCode.Code} успешно применен!",
                discount = promoCode.DiscountAmount,
                code = promoCode.Code
            });
        }

        // --- 6. ОФОРМЛЕНИЕ ЗАКАЗА ---
        [HttpPost]
        public async Task<IActionResult> Checkout(string phone, string social, string address, string appliedPromoCode)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
            {
                TempData["Error"] = "Пожалуйста, заполните номер телефона и адрес для доставки.";
                return RedirectToAction("Index");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdString);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index");

            foreach (var item in cartItems)
            {
                if (item.Quantity > item.Product.StockQuantity)
                {
                    TempData["Error"] = $"Товар «{item.Product.Title}» закончился или доступен в меньшем количестве. Пожалуйста, обновите корзину.";
                    return RedirectToAction("Index");
                }
            }

            decimal totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            if (!string.IsNullOrWhiteSpace(appliedPromoCode))
            {
                string cleanCode = appliedPromoCode.Trim().ToUpper();
                var promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == cleanCode && p.IsActive);

                if (promo != null && promo.UsedCount < promo.MaxUses && totalAmount >= promo.MinOrderAmount)
                {
                    totalAmount -= promo.DiscountAmount;
                    if (totalAmount < 0) totalAmount = 0;

                    promo.UsedCount += 1;
                }
            }

            var order = new Order
            {
                UserID = userId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount, 
                Status = "Новый",
                Phone = phone,
                SocialLink = !string.IsNullOrWhiteSpace(social) ? social : "Не указано",
                Address = address
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });

                item.Product.StockQuantity -= item.Quantity;
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ваш заказ успешно оформлен!";
            return RedirectToAction("Profile", "User");
        }
    }
}