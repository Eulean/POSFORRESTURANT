using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<MenuItemIngredient> MenuItemIngredients => Set<MenuItemIngredient>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<IngredientStockAdjustment> IngredientStockAdjustments => Set<IngredientStockAdjustment>();
    public DbSet<DailyCloseSummary> DailyCloseSummaries => Set<DailyCloseSummary>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MenuItemIngredient>()
            .HasIndex(x => new { x.MenuItemId, x.IngredientId })
            .IsUnique();

        builder.Entity<DiningTable>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<MenuCategory>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<MenuItem>()
            .HasIndex(x => x.Name);

        builder.Entity<Ingredient>()
            .HasIndex(x => x.Name);

        builder.Entity<Order>()
            .HasIndex(x => x.CreatedAt);

        builder.Entity<DailyCloseSummary>()
            .HasIndex(x => x.DateUtc)
            .IsUnique();
    }
}
