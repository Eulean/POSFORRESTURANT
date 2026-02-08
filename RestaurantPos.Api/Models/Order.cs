using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPos.Api.Models;

public class Order
{
    public int Id { get; set; }

    public int DiningTableId { get; set; }
    public DiningTable DiningTable { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public string? WaiterId { get; set; }
    public ApplicationUser? Waiter { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
