using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPos.Api.Models;

public class MenuItemIngredient
{
    public int Id { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantity { get; set; }
}
