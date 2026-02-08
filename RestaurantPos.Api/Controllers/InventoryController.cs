using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = Roles.Admin)]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public InventoryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("adjust")]
    public async Task<ActionResult> AdjustStock(StockAdjustmentRequest request)
    {
        var ingredient = await _db.Ingredients.FindAsync(request.IngredientId);
        if (ingredient == null)
        {
            return NotFound();
        }

        ingredient.StockQuantity += request.QuantityChange;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _db.IngredientStockAdjustments.Add(new IngredientStockAdjustment
        {
            IngredientId = ingredient.Id,
            QuantityChange = request.QuantityChange,
            Reason = request.Reason.Trim(),
            UserId = userId
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<IngredientDto>>> GetLowStock()
    {
        var items = await _db.Ingredients
            .Where(i => i.StockQuantity <= i.ReorderLevel)
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto(i.Id, i.Name, i.Unit, i.StockQuantity, i.ReorderLevel, i.CostPerUnit, i.IsActive))
            .ToListAsync();

        return Ok(items);
    }
}
