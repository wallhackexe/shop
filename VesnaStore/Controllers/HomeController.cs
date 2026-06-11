using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VesnaStore.Data;
using VesnaStore.Models;

namespace VesnaStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // --- ИНФОРМАЦИОННЫЕ СТРАНИЦЫ ---
        public IActionResult Cookies() => View();
        public IActionResult Delivery() => View();
        public IActionResult Returns() => View();
        public IActionResult Collections() => View();
        public IActionResult Privacy() => View();

        // --- ГЛАВНАЯ СТРАНИЦА ---
        public async Task<IActionResult> Index()
        {
            // Получаем список ID товаров в избранном у пользователя (если он авторизован)
            ViewBag.UserFavorites = await GetUserFavoriteIdsAsync();

            // Загружаем 8 последних опубликованных новинок
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsPublished && p.IsNew)
                .OrderByDescending(p => p.ProductID)
                .Take(8)
                .ToListAsync();

            return View(products);
        }

        // --- КАРТОЧКА ТОВАРA ---
        public async Task<IActionResult> Details(int id)
        {
            // Загружаем товар и только одобренные модератором отзывы (Filtered Include)
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsPublished);

            if (product == null)
                return NotFound();

            // Похожие товары: из той же категории, в наличии, исключая текущий. Сортировка — рандом.
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id && p.IsPublished && p.StockQuantity > 0)
                .OrderBy(p => Guid.NewGuid())
                .Take(4)
                .ToListAsync();

            return View(product);
        }

        // --- КАТАЛОГ С ФИЛЬТРАМИ ---
        public async Task<IActionResult> Catalog(int? categoryId, string size, string brand, string search, string sort)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsPublished)
                .AsQueryable();

            // 1. Фильтр по размеру (умное сопоставление сеток)
            if (!string.IsNullOrEmpty(size))
            {
                string upperSize = size.Trim().ToUpper();
                var sizeVariants = new List<string> { upperSize };

                var sizeGroups = new List<string[]>
                {
                    new[] { "XS", "40", "34" },
                    new[] { "S", "42", "36" },
                    new[] { "M", "44", "38" },
                    new[] { "L", "46", "40" },
                    new[] { "XL", "48", "42" },
                    new[] { "XXL", "50", "44" },
                    new[] { "XXXL", "52", "46" }
                };

                var matchedGroup = sizeGroups.FirstOrDefault(g => g.Contains(upperSize));
                if (matchedGroup != null)
                {
                    foreach (var s in matchedGroup)
                    {
                        if (!sizeVariants.Contains(s))
                            sizeVariants.Add(s);
                    }
                }

                query = query.Where(p => p.Sizes != null && sizeVariants.Any(v => p.Sizes.Contains(v)));
            }

            // 2. Фильтр по категории
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // 3. Фильтр по бренду
            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand.BrandName == brand);
            }

            // 4. Поиск по названию или артикулу
            if (!string.IsNullOrEmpty(search))
            {
                string cleanSearch = search.Trim();
                query = query.Where(p => p.Title.Contains(cleanSearch) || p.ArticleNumber.Contains(cleanSearch));
            }

            // 5. Сортировка
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // Данные для боковой панели / фильтров в представлении
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.UserFavorites = await GetUserFavoriteIdsAsync();

            var products = await query.ToListAsync();
            return View(products);
        }

        // --- СТРАНИЦА ОШИБОК ---
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.GetValueOrDefault() == 404)
                return View("NotFound");

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (DRY) ---
        private async Task<List<int>> GetUserFavoriteIdsAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return new List<int>();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                return await _context.Favorites
                    .Where(f => f.UserID == userId)
                    .Select(f => f.ProductID)
                    .ToListAsync();
            }

            return new List<int>();
        }
    }
}