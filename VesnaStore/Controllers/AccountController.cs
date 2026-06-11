using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // --- ЛИЧНЫЙ КАБИНЕТ (ЛИЧНЫЕ ДАННЫЕ, ИЗБРАННОЕ И ЗАКАЗЫ) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login");

            // Загружаем пользователя
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToAction("Login");

            // Получаем список ID избранных товаров
            var favoriteIds = await _context.Favorites
                .Where(f => f.UserID == userId)
                .Select(f => f.ProductID)
                .ToListAsync();

            // Загружаем сами товары из избранного с категориями и отзывами
            var favoriteProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => favoriteIds.Contains(p.ProductID))
                .ToListAsync();

            // Загружаем историю заказов
            var orders = await _context.Orders
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Передаем всё в ViewBag без декомпилированного мусора CallSite
            ViewBag.Favorites = favoriteProducts;
            ViewBag.Orders = orders;
            ViewBag.FavoriteIds = favoriteIds;

            return View(user);
        }

        // --- РЕГИСТРАЦИЯ ---
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string fullName)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "Этот email уже зарегистрирован");
                return View();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = password,
                FullName = fullName,
                UserRole = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Регистрация прошла успешно! Теперь вы можете войти.";
            return RedirectToAction("Login");
        }

        // --- СИНХРОНИЗАЦИЯ КОРЗИНЫ ---
        [HttpPost]
        public async Task<IActionResult> SyncCart([FromBody] List<int> productIds)
        {
            if (!User.Identity.IsAuthenticated || productIds == null || !productIds.Any())
                return Json(new { success = false });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Json(new { success = false });

            int userId = int.Parse(userIdClaim);

            foreach (var pid in productIds)
            {
                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == pid);

                if (existing == null)
                {
                    _context.CartItems.Add(new CartItem { UserID = userId, ProductID = pid, Quantity = 1 });
                }
            }

            await _context.SaveChangesAsync();

            int totalCount = await _context.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => (int?)c.Quantity) ?? 0;

            return Json(new { success = true, cartCount = totalCount });
        }

        // --- ИЗБРАННОЕ (ДОБАВИТЬ / УДАЛИТЬ) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            try
            {
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.ProductID == productId && f.UserID == userId);

                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();
                    return Ok(new { status = "removed" });
                }

                _context.Favorites.Add(new Favorite
                {
                    ProductID = productId,
                    UserID = userId
                });

                await _context.SaveChangesAsync();
                return Ok(new { status = "added" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // --- ВХОД (LOGIN) ---
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

            if (user != null)
            {
                HttpContext.Session.Remove("LoginAttempts");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Role, user.UserRole),
                    new Claim("FullName", user.FullName ?? "")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Success"] = "Вы успешно вошли в систему!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            int attempts = HttpContext.Session.GetInt32("LoginAttempts") ?? 0;
            attempts++;
            HttpContext.Session.SetInt32("LoginAttempts", attempts);

            if (attempts >= 5)
            {
                TempData["Error"] = "Достигнут лимит попыток (5). Попробуйте восстановить пароль или зайдите позже.";
            }
            else
            {
                TempData["Error"] = $"Неверный логин или пароль. Попытка {attempts} из 5.";
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // --- ВЫХОД (LOGOUT) ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}