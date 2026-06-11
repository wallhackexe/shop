using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VesnaStore.Data; // Проверь, чтобы NameSpace совпадал с твоим
using VesnaStore.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка контроллеров и видов
builder.Services.AddControllersWithViews();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Настройки блокировки
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
// 2. Подключение к SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5, // Максимальное количество попыток переподключения
                maxRetryDelay: TimeSpan.FromSeconds(10), // Пауза между попытками
                errorNumbersToAdd: null); // Стандартные ошибки сети
        }));

// 3. Настройка безопасности (Cookies)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/Index";
    });
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Время жизни сессии
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Код видим только когда ты запускаешь проект у себя
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Чтобы работали CSS и JS из wwwroot
app.UseRouting();

app.UseSession();
app.UseAuthentication(); // Кто ты?
app.UseAuthorization();  // Что тебе можно?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();