namespace RestaurantPos.Api.DTOs;

public record DashboardSummaryDto(
    int OpenOrders,
    int KitchenQueue,
    int OccupiedTables,
    int TotalTables,
    int LowStockCount
);
