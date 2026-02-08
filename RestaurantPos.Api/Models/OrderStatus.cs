namespace RestaurantPos.Api.Models;

public enum OrderStatus
{
    Open = 1,
    InProgress = 2,
    Ready = 3,
    Served = 4,
    Paid = 5,
    Closed = 6,
    Cancelled = 7
}
