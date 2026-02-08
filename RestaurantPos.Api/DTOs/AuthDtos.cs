namespace RestaurantPos.Api.DTOs;

public record RegisterRequest(string UserName, string Password, string? DisplayName, string Role);

public record LoginRequest(string UserName, string Password);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, string UserId, string UserName, string[] Roles);
