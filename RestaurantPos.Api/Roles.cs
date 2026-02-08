namespace RestaurantPos.Api;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Waiter = "Waiter";
    public const string Cashier = "Cashier";

    public static readonly string[] All = [Admin, Waiter, Cashier];
}
