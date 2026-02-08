using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPos.Api.Models;

public class Ingredient
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Unit { get; set; } = "each";

    [Column(TypeName = "decimal(18,3)")]
    public decimal StockQuantity { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal ReorderLevel { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CostPerUnit { get; set; }

    public bool IsActive { get; set; } = true;

    public List<MenuItemIngredient> RecipeItems { get; set; } = new();
    public List<IngredientStockAdjustment> StockAdjustments { get; set; } = new();
}
