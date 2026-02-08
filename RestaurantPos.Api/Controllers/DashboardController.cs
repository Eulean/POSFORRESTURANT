using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var openOrders = await _db.Orders.CountAsync(o => o.Status != OrderStatus.Closed && o.Status != OrderStatus.Cancelled);
        var kitchenQueue = await _db.Orders.CountAsync(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.Ready);
        var totalTables = await _db.DiningTables.CountAsync();
        var occupiedTables = await _db.DiningTables.CountAsync(t => !t.IsAvailable);
        var lowStock = await _db.Ingredients.CountAsync(i => i.StockQuantity <= i.ReorderLevel);

        return Ok(new DashboardSummaryDto(openOrders, kitchenQueue, occupiedTables, totalTables, lowStock));
    }
}
