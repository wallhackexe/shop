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
            ViewBag.UserFavorites = await GetUserFavoriteIdsAsync();

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
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsPublished);

            if (product == null)
                return NotFound();

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
        public async Task<IActionResult> Catalog(int? categoryId, string size, string brand, string search, string sort, decimal? minPrice, decimal? maxPrice)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsPublished)
                .AsQueryable();

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

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand.BrandName == brand);
            }

            if (!string.IsNullOrEmpty(search))
            {
                string cleanSearch = search.Trim();
                query = query.Where(p => p.Title.Contains(cleanSearch) || p.ArticleNumber.Contains(cleanSearch));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.UserFavorites = await GetUserFavoriteIdsAsync();

            var products = await query.ToListAsync();
            return View(products);
        }

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