namespace RestaurantPos.Api.DTOs;

public record TopItemDto(int MenuItemId, string Name, int Quantity);

public record PaymentMethodDto(string Method, decimal Total);

public record HourlyCountDto(int Hour, int Orders);

public record DailyReportDto(
    DateTime DateUtc,
    int OrdersToday,
    decimal TotalSales,
    decimal IngredientCost,
    decimal Profit,
    List<TopItemDto> TopItems,
    List<PaymentMethodDto> RevenueByMethod,
    HourlyCountDto? BusiestHour
);

public record DailyBreakdownDto(DateTime DateUtc, int Orders, decimal TotalSales, decimal IngredientCost, decimal Profit);

public record MonthlyReportDto(
    DateTime StartUtc,
    DateTime EndUtc,
    int OrdersTotal,
    decimal TotalSales,
    decimal IngredientCost,
    decimal Profit,
    List<TopItemDto> TopItems,
    List<PaymentMethodDto> RevenueByMethod,
    List<DailyBreakdownDto> DailyBreakdown
);

public record CloseDayRequest(DateTime? DateUtc);
