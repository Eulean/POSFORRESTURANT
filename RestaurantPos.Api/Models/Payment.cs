using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPos.Api.Models;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(30)]
    public string Method { get; set; } = "Cash";

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Reference { get; set; }
}
