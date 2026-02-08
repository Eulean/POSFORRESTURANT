using System.ComponentModel.DataAnnotations;

namespace RestaurantPos.Api.Models;

public class MenuCategory
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public List<MenuItem> MenuItems { get; set; } = new();
}
