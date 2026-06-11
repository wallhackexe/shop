using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VesnaStore.Data;
using VesnaStore.Models;

namespace VesnaStore.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // --- 1. ПРОФИЛЬ ПОЛЬЗОВАТЕЛЯ ---
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            // Загружаем уведомления
            ViewBag.Notifications = await _context.UserNotifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Загружаем данные пользователя вместе с историей заказов
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound();

            // Загружаем избранные товары
            var favorites = await _context.Favorites
                .Where(f => f.UserID == userId)
                .Include(f => f.Product).ThenInclude(p => p.Reviews)
                .Include(f => f.Product).ThenInclude(p => p.Category)
                .Select(f => f.Product)
                .ToListAsync();

            ViewBag.FavoriteIds = favorites.Select(p => p.ProductID).ToList();
            ViewBag.FavoriteProducts = favorites;

            return View(user);
        }

        // --- 2. ОБНОВЛЕНИЕ ИМЕНИ ---
        [HttpPost]
        public async Task<IActionResult> UpdateName(string fullName)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null || string.IsNullOrWhiteSpace(fullName))
                return Json(new { success = false, message = "Ошибка при обновлении имени" });

            user.FullName = fullName.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Имя успешно обновлено" });
        }

        // --- 3. СМЕНА ПАРОЛЯ ---
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user.PasswordHash != oldPass)
                return Json(new { success = false, message = "Старый пароль неверен" });

            if (newPass != confirmPass)
                return Json(new { success = false, message = "Пароли не совпадают" });

            user.PasswordHash = newPass;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Пароль изменен" });
        }

        // --- 4. СМЕНА EMAIL (Восстановлено из декомпилята) ---
        [HttpPost]
        public async Task<IActionResult> ChangeEmail(string newEmail, string code)
        {
            // В декомпилированной версии стояла жесткая заглушка на код "123456"
            if (code != "123456")
                return Json(new { success = false, message = "Неверный код подтверждения" });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.Email = newEmail;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Email успешно изменен" });
        }

        // --- 5. ОТПРАВКА ОТЗЫВА ---
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment, string? imageUrl)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Json(new { success = false, message = "Сессия истекла. Войдите заново." });

            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Пожалуйста, заполните текст отзыва." });

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            var review = new Review
            {
                ProductID = productId,
                UserID = userId,
                UserName = user?.FullName ?? "Аноним",
                Comment = comment.Trim(),
                Rating = rating,
                IsApproved = true, // В оригинале отзывы одобряются автоматически
                CreatedAt = DateTime.Now,
                ReviewImageUrl = imageUrl
            };

            try
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Запись ошибки в консоль, как в оригинале
                Console.WriteLine("[Review DB Error]: " + ex.Message);
                return Json(new { success = false, message = "Ошибка при сохранении отзыва в базу данных." });
            }
        }
    }
}