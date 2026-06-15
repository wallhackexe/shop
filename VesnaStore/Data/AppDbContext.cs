using Microsoft.EntityFrameworkCore;
using VesnaStore.Models;

#nullable enable
namespace VesnaStore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext((DbContextOptions)options)
{
    public DbSet<CartItem> CartItems { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<MediaFile> MediaFiles { get; set; }

    public DbSet<Brand> Brands { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<Favorite> Favorites { get; set; }

    public DbSet<UserNotification> UserNotifications { get; set; }

    public DbSet<CategorySizeTemplate> CategorySizeTemplates { get; set; }

    public DbSet<ProductSizeValue> ProductSizeValues { get; set; }

    public DbSet<PromoCode> PromoCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().ToTable<User>("Users");
        modelBuilder.Entity<Product>().ToTable<Product>("Products");
        modelBuilder.Entity<Category>().ToTable<Category>("Categories");
    }
}
