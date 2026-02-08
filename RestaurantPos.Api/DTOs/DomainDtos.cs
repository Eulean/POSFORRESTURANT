namespace RestaurantPos.Api.DTOs;

public record MenuCategoryDto(int Id, string Name, bool IsActive, int SortOrder);

public record MenuCategoryCreateRequest(string Name, bool IsActive, int SortOrder);

public record MenuItemDto(int Id, string Name, string? Description, string? ImageUrl, decimal Price, bool IsActive, int? CategoryId);

public record MenuItemCreateRequest(string Name, string? Description, string? ImageUrl, decimal Price, bool IsActive, int? CategoryId);

public record MenuItemIngredientRequest(int IngredientId, decimal Quantity);

public record MenuItemIngredientDto(int Id, int IngredientId, string IngredientName, decimal Quantity);

public record IngredientDto(int Id, string Name, string Unit, decimal StockQuantity, decimal ReorderLevel, decimal CostPerUnit, bool IsActive);

public record IngredientCreateRequest(string Name, string Unit, decimal StockQuantity, decimal ReorderLevel, decimal CostPerUnit, bool IsActive);

public record DiningTableDto(int Id, string Name, int Capacity, bool IsAvailable);

public record DiningTableCreateRequest(string Name, int Capacity, bool IsAvailable);

public record OrderItemCreateRequest(int MenuItemId, int Quantity);

public record OrderCreateRequest(int DiningTableId, List<OrderItemCreateRequest> Items);

public record OrderItemDto(int Id, int MenuItemId, string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderDto(int Id, int DiningTableId, string TableName, string Status, decimal Subtotal, decimal TotalAmount, DateTime CreatedAt, DateTime UpdatedAt, List<OrderItemDto> Items);

public record OrderStatusUpdateRequest(string Status);

public record PaymentCreateRequest(decimal Amount, string Method, string? Reference);

public record StockAdjustmentRequest(int IngredientId, decimal QuantityChange, string Reason);
