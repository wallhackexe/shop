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
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            ViewBag.Notifications = await _context.UserNotifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound();

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

        // --- 4. СМЕНА EMAIL  ---
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var message = new System.Net.Mail.MailMessage())
                {
                    message.To.Add(new System.Net.Mail.MailAddress(toEmail));
                    message.From = new System.Net.Mail.MailAddress("fogetw@gmail.com", "VESNA Store");
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new System.Net.Mail.SmtpClient("smtp.gmail.com"))
                    {
                        client.Port = 587; 
                        client.Credentials = new System.Net.NetworkCredential("fogetw@gmail.com", "bdsdrcwoguatkecs");
                        client.EnableSsl = true;
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка отправки почты: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ChangeEmail(string newEmail, string code)
        {
            string sessionCode = HttpContext.Session.GetString("EmailChangeCode");
            string sessionEmail = HttpContext.Session.GetString("NewEmailCandidate");

            if (string.IsNullOrEmpty(sessionCode) || sessionCode != code?.Trim() || sessionEmail != newEmail?.Trim())
            {
                return Json(new { success = false, message = "Неверный код подтверждения или Email" });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.Email = newEmail;
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("EmailChangeCode");
                HttpContext.Session.Remove("NewEmailCandidate");
            }

            return Json(new { success = true, newEmail = newEmail, message = "Email успешно изменен" });
        }
        [HttpPost]
        public async Task<IActionResult> RequestEmailChangeCode(string newEmail)
        {
            if (await _context.Users.AnyAsync(u => u.Email == newEmail))
                return Json(new { success = false, message = "Этот email уже занят." });

            string code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("EmailChangeCode", code);
            HttpContext.Session.SetString("NewEmailCandidate", newEmail);

            await SendEmailAsync(newEmail, "Подтверждение смены почты", $"Ваш код подтверждения: <b>{code}</b>");

            return Json(new { success = true });
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
                IsApproved = true,
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
                Console.WriteLine("[Review DB Error]: " + ex.Message);
                return Json(new { success = false, message = "Ошибка при сохранении отзыва в базу данных." });
            }
        }
    }
}