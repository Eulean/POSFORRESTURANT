namespace RestaurantPos.Api.Models;

public class ShopProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "GALA taste";
    public string Address { get; set; } = "123 Main Street, City";
    public string Phone { get; set; } = "(000) 000-0000";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
