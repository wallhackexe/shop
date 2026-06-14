using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VesnaStore.Data;
using VesnaStore.Models;

namespace VesnaStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsPublished)
                .ToListAsync();

            ViewBag.TotalCategoriesCount = await _context.Categories.CountAsync();
            ViewBag.TotalOrdersCount = await _context.Orders.CountAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetSizeTemplate(int categoryId)
        {
            var template = await _context.CategorySizeTemplates.FirstOrDefaultAsync(t => t.CategoryID == categoryId);
            if (template == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                labelA = template.LabelA,
                labelB = template.LabelB,
                labelC = template.LabelC,
                imagePath = template.BaseTemplateImagePath
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Используем SelectList, чтобы HTML-хелпер понимал, что показывать и что сохранять
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName");

            ViewBag.BrandsList = await _context.Brands.ToListAsync();
            ViewBag.TotalOrdersCount = await _context.Orders.CountAsync();

            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("ProductID,Title,Price,ImageURL,AdditionalImages,Description,CategoryID,Sizes,Material,Color,CreatedAt,StockQuantity,ArticleNumber,Season,IsPublished,IsNew,SizeValueA,SizeValueB,SizeValueC,StartX_A,StartY_A,EndX_A,EndY_A,TextX_A,TextY_A,StartX_B,StartY_B,EndX_B,EndY_B,TextX_B,TextY_B,StartX_C,StartY_C,EndX_C,EndY_C,TextX_C,TextY_C")] Product product,
    string brandName, string brandCountry, // <-- ВЕРНУЛИ ЭТИ ПОЛЯ
    string SizeValueA, string SizeValueB, string SizeValueC)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Brand");

            if (ModelState.IsValid)
            {
                // 1. ЛОГИКА БРЕНДА: Ищем существующий или создаем новый
                if (!string.IsNullOrWhiteSpace(brandName))
                {
                    var existingBrand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brandName.Trim().ToLower());

                    if (existingBrand == null)
                    {
                        existingBrand = new Brand
                        {
                            BrandName = brandName.Trim(),
                            BrandCountry = !string.IsNullOrWhiteSpace(brandCountry) ? brandCountry.Trim() : "Россия"
                        };
                        _context.Brands.Add(existingBrand);
                        await _context.SaveChangesAsync(); // Сохраняем новый бренд в БД
                    }
                    product.BrandID = existingBrand.BrandID; // Привязываем товар к бренду
                }

                // 2. Обработка Инфографики (ImgBB)
                if (!string.IsNullOrEmpty(product.ImageURL))
                {
                    try
                    {
                        string imgBbUrl = await UploadToImgBBAndCleanUp(product.ImageURL);
                        if (!string.IsNullOrEmpty(imgBbUrl))
                        {
                            if (string.IsNullOrEmpty(product.AdditionalImages))
                                product.AdditionalImages = imgBbUrl;
                            else
                                product.AdditionalImages = product.AdditionalImages.TrimEnd('\r', '\n') + "\n" + imgBbUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Товар создан, но копия фото для размеров не залилась на ImgBB: {ex.Message}";
                    }
                }

                // 3. Сохраняем товар
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 4. Сохраняем текстовые замеры
                var sizeValue = new ProductSizeValue
                {
                    ProductID = product.ProductID,
                    ValueA = string.IsNullOrWhiteSpace(SizeValueA) ? "-" : SizeValueA,
                    ValueB = string.IsNullOrWhiteSpace(SizeValueB) ? "-" : SizeValueB,
                    ValueC = string.IsNullOrWhiteSpace(SizeValueC) ? "-" : SizeValueC
                };
                _context.ProductSizeValues.Add(sizeValue);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успешно добавлен!";
                return RedirectToAction(nameof(Create));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name");
            ViewBag.BrandsList = await _context.Brands.ToListAsync();
            return View(product);
        }

        // НОВЫЙ МЕТОД ДЛЯ УДАЛЕНИЯ БРЕНДА ИЗ БД
        [HttpPost]
        public async Task<IActionResult> DeleteBrand(string brandName)
        {
            if (string.IsNullOrWhiteSpace(brandName))
                return Json(new { success = false, message = "Пустое имя." });

            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandName.ToLower() == brandName.Trim().ToLower());

            if (brand != null)
            {
                // Проверяем, есть ли товары с этим брендом
                bool hasProducts = await _context.Products.AnyAsync(p => p.BrandID == brand.BrandID);
                if (hasProducts)
                    return Json(new { success = false, message = "Нельзя удалить бренд, так как к нему привязаны товары!" });

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Бренд не найден в БД." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.BrandsList = await _context.Brands.ToListAsync(); // Заменил на BrandsList
            ViewBag.TotalOrdersCount = await _context.Orders.CountAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, string brandName, string brandCountry)
        {
            if (id != product.ProductID) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("Brand");

            if (ModelState.IsValid)
            {
                // 1. Обработка Инфографики
                if (!string.IsNullOrEmpty(product.ImageURL))
                {
                    string infographicUrl = await UploadToImgBBAndCleanUp(product.ImageURL);
                    if (!string.IsNullOrEmpty(infographicUrl))
                    {
                        var images = string.IsNullOrEmpty(product.AdditionalImages)
                            ? new List<string>()
                            : product.AdditionalImages.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        images = images.Where(img => !img.Contains("imgbb.com") && !img.Contains("ibb.co")).ToList();
                        images.Add(infographicUrl);
                        product.AdditionalImages = string.Join("\n", images);
                    }
                }

                // 2. Обработка Бренда (как в Create: ищем существующий или создаем новый)
                if (!string.IsNullOrWhiteSpace(brandName))
                {
                    var existingBrand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brandName.Trim().ToLower());

                    if (existingBrand == null)
                    {
                        existingBrand = new Brand
                        {
                            BrandName = brandName.Trim(),
                            BrandCountry = !string.IsNullOrWhiteSpace(brandCountry) ? brandCountry.Trim() : "Россия"
                        };
                        _context.Brands.Add(existingBrand);
                        await _context.SaveChangesAsync();
                    }
                    product.BrandID = existingBrand.BrandID;
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Inventory));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.BrandsList = await _context.Brands.ToListAsync(); // Возвращаем список при ошибке
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsPublished = false;
                product.StockQuantity = 0;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleNew(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.IsNew = !product.IsNew;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Inventory));
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var exists = await _context.Categories.AnyAsync(c => c.Name == name);
                if (!exists)
                {
                    _context.Categories.Add(new Category { Name = name });
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category != null && !category.Products.Any())
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        // --- СКЛАДСКОЙ УЧЕТ ---
        public async Task<IActionResult> Inventory(string search, int? categoryId, string status)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) ||
                                         p.ArticleNumber.Contains(search) ||
                                         (p.Brand != null && p.Brand.BrandName.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId);
            }

            if (status == "none") query = query.Where(p => p.StockQuantity <= 0);
            else if (status == "low") query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity < 5);

            var products = await query.ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TotalOrdersCount = await _context.Orders.CountAsync();
            ViewBag.TotalStockValue = products.Sum(p => p.Price * p.StockQuantity);
            ViewBag.TotalItems = products.Sum(p => p.StockQuantity);

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> TogglePublish(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.IsPublished = !product.IsPublished;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Inventory));
        }

        [HttpPost]
        public async Task<IActionResult> AdjustStock(int productId, int adjustment)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.StockQuantity += adjustment;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Inventory));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productId, int stockQuantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.StockQuantity = stockQuantity;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Inventory));
        }

        public async Task<IActionResult> Media()
        {
            var files = await _context.MediaFiles.OrderByDescending(m => m.UploadedAt).ToListAsync();
            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMediaUrl([FromBody] string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                _context.MediaFiles.Add(new MediaFile { Url = url });
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status, string trackingNumber)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.TrackingNumber = trackingNumber;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> ProxyUpload(IFormFile image)
        {
            var apiKey = _configuration["ApiKeys:ImgBB"];
            using var client = new HttpClient();
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(image.OpenReadStream());
            content.Add(fileContent, "image", image.FileName);

            var response = await client.PostAsync($"https://api.imgbb.com/1/upload?key={apiKey}", content);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> SendMailing(string title, string message, string targetAudience)
        {
            var users = await _context.Users.ToListAsync();
            foreach (var u in users)
            {
                _context.UserNotifications.Add(new UserNotification { UserID = u.UserID, Title = title, Message = message, CreatedAt = DateTime.Now });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("MailingPanel");
        }

        [HttpGet]
        public IActionResult MailingPanel() => View();

        [HttpGet]
        public async Task<IActionResult> PromoCodes() => View(await _context.PromoCodes.ToListAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromoCode(string Code, decimal DiscountAmount, decimal MinOrderAmount, int MaxUses)
        {
            _context.PromoCodes.Add(new PromoCode { Code = Code.ToUpper(), DiscountAmount = DiscountAmount, MinOrderAmount = MinOrderAmount, MaxUses = MaxUses });
            await _context.SaveChangesAsync();
            return RedirectToAction("PromoCodes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromoCode(int id)
        {
            var p = await _context.PromoCodes.FindAsync(id);
            if (p != null) { _context.PromoCodes.Remove(p); await _context.SaveChangesAsync(); }
            return RedirectToAction("PromoCodes");
        }

        [HttpGet]
        public async Task<IActionResult> Reviews() => View(await _context.Reviews.Include(r => r.Product).ToListAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReviewVisibility(int id)
        {
            var r = await _context.Reviews.FindAsync(id);
            if (r != null)
            {
                r.IsApproved = !r.IsApproved;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isVisible = r.IsApproved,
                    message = r.IsApproved ? "Отзыв опубликован" : "Отзыв скрыт"
                });
            }

            return Json(new { success = false, message = "Отзыв не найден" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.MediaFiles.FindAsync(id);
            if (media != null)
            {
                _context.MediaFiles.Remove(media);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Файл не найден" });
        }
        // --- УТИЛИТА ДЛЯ ЗАГРУЗКИ IMGBB ---
        private async Task<string> UploadToImgBBAndCleanUp(string imageSource)
        {
            try
            {
                var apiKey = _configuration["ApiKeys:ImgBB"];
                using var client = new HttpClient();
                byte[] fileBytes;

                if (imageSource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    fileBytes = await client.GetByteArrayAsync(imageSource);
                }
                else
                {
                    fileBytes = await System.IO.File.ReadAllBytesAsync(imageSource);
                }

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                content.Add(fileContent, "image", "infographic.png");

                var response = await client.PostAsync($"https://api.imgbb.com/1/upload?key={apiKey}", content);
                if (response.IsSuccessStatusCode)
                {
                    var res = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(res);
                    return doc.RootElement.GetProperty("data").GetProperty("url").GetString();
                }
            }
            catch { /* логгируем ошибку при необходимости */ }
            return null;
        }
    }
}