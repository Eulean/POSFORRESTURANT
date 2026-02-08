using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPos.Api.Models;

public class DailyCloseSummary
{
    public int Id { get; set; }

    public DateTime DateUtc { get; set; }

    public int OrdersToday { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IngredientCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Profit { get; set; }

    public string TopItemsJson { get; set; } = "[]";

    public string RevenueByMethodJson { get; set; } = "[]";

    public int? BusiestHour { get; set; }

    public int? BusiestHourOrders { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
