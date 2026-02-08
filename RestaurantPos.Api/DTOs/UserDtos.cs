namespace RestaurantPos.Api.DTOs;

public record ResetPasswordRequest(string UserId, string NewPassword);
