using System.ComponentModel.DataAnnotations;

namespace RestaurantPos.Api.Models;

public class DiningTable
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsAvailable { get; set; } = true;

    public List<Order> Orders { get; set; } = new();
}
