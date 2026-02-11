using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        var hasCoreData = db.MenuItems.Any() || db.DiningTables.Any() || db.Ingredients.Any();

        if (!db.ShopProfiles.Any())
        {
            db.ShopProfiles.Add(new ShopProfile
            {
                Name = "GALA taste",
                Address = "123 Main Street, City",
                Phone = "(000) 000-0000",
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (hasCoreData)
        {
            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }
            return;
        }

        var mains = new MenuCategory { Name = "Mains", SortOrder = 1, IsActive = true };
        var drinks = new MenuCategory { Name = "Drinks", SortOrder = 2, IsActive = true };
        var dessert = new MenuCategory { Name = "Dessert", SortOrder = 3, IsActive = true };

        db.MenuCategories.AddRange(mains, drinks, dessert);

        var salmon = new MenuItem
        {
            Name = "Cedar Salmon",
            Description = "Herb crust, lemon glaze",
            Price = 24.50m,
            IsActive = true,
            Category = mains
        };
        var risotto = new MenuItem
        {
            Name = "Garden Risotto",
            Description = "Seasonal veg, parmesan",
            Price = 18.00m,
            IsActive = true,
            Category = mains
        };
        var tart = new MenuItem
        {
            Name = "Citrus Tart",
            Description = "Vanilla cream",
            Price = 8.50m,
            IsActive = true,
            Category = dessert
        };
        var latte = new MenuItem
        {
            Name = "Spiced Latte",
            Description = "Cinnamon, oat milk",
            Price = 5.75m,
            IsActive = true,
            Category = drinks
        };

        db.MenuItems.AddRange(salmon, risotto, tart, latte);

        var tomatoes = new Ingredient { Name = "Tomatoes", Unit = "kg", StockQuantity = 2.1m, ReorderLevel = 1.0m, CostPerUnit = 3.50m, IsActive = true };
        var salmonFillet = new Ingredient { Name = "Salmon", Unit = "fillet", StockQuantity = 6m, ReorderLevel = 3m, CostPerUnit = 5.25m, IsActive = true };
        var rice = new Ingredient { Name = "Arborio Rice", Unit = "kg", StockQuantity = 4m, ReorderLevel = 1.5m, CostPerUnit = 2.40m, IsActive = true };
        var parmesan = new Ingredient { Name = "Parmesan", Unit = "kg", StockQuantity = 1.8m, ReorderLevel = 1.0m, CostPerUnit = 7.80m, IsActive = true };
        var coffee = new Ingredient { Name = "Espresso Beans", Unit = "kg", StockQuantity = 2.4m, ReorderLevel = 1.0m, CostPerUnit = 12.00m, IsActive = true };

        db.Ingredients.AddRange(tomatoes, salmonFillet, rice, parmesan, coffee);

        db.MenuItemIngredients.AddRange(
            new MenuItemIngredient { MenuItem = salmon, Ingredient = salmonFillet, Quantity = 1 },
            new MenuItemIngredient { MenuItem = salmon, Ingredient = tomatoes, Quantity = 0.2m },
            new MenuItemIngredient { MenuItem = risotto, Ingredient = rice, Quantity = 0.15m },
            new MenuItemIngredient { MenuItem = risotto, Ingredient = parmesan, Quantity = 0.05m },
            new MenuItemIngredient { MenuItem = latte, Ingredient = coffee, Quantity = 0.03m }
        );

        db.DiningTables.AddRange(
            new DiningTable { Name = "Table 1", Capacity = 2, IsAvailable = true },
            new DiningTable { Name = "Table 2", Capacity = 4, IsAvailable = true },
            new DiningTable { Name = "Table 3", Capacity = 4, IsAvailable = true },
            new DiningTable { Name = "Table 4", Capacity = 6, IsAvailable = true },
            new DiningTable { Name = "Table 5", Capacity = 2, IsAvailable = true },
            new DiningTable { Name = "Table 6", Capacity = 6, IsAvailable = true }
        );

        await db.SaveChangesAsync();
    }
}
