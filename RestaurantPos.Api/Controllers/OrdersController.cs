using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll([FromQuery] OrderStatus? status)
    {
        var query = _db.Orders
            .Include(o => o.DiningTable)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.DiningTableId,
                o.DiningTable.Name,
                o.Status.ToString(),
                o.Subtotal,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(i.Id, i.MenuItemId, i.MenuItem.Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.DiningTable)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(new OrderDto(
            order.Id,
            order.DiningTableId,
            order.DiningTable.Name,
            order.Status.ToString(),
            order.Subtotal,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.MenuItemId, i.MenuItem.Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()));
    }

    [HttpGet("{id:int}/receipt")]
    [Authorize]
    public async Task<IActionResult> GetReceipt(int id)
    {
        var order = await _db.Orders
            .Include(o => o.DiningTable)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var paidTotal = order.Payments.Sum(p => p.Amount);
        var balance = order.TotalAmount - paidTotal;
        var lastPayment = order.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A5);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text("GALA").FontSize(20).Bold();
                    column.Item().Text("Restaurant POS").FontSize(12).SemiBold();
                    column.Item().Text("123 Main Street, City").FontSize(10);
                    column.Item().Text("Phone: (000) 000-0000").FontSize(10);
                    column.Item().Text($"Receipt #{order.Id}").FontSize(12).Bold();
                    column.Item().Text($"Table: {order.DiningTable.Name}");
                    column.Item().Text($"Status: {order.Status}");
                    column.Item().Text($"Opened: {order.CreatedAt:yyyy-MM-dd HH:mm}");
                    if (lastPayment != null)
                    {
                        column.Item().Text($"Last Payment: {lastPayment.PaidAt:yyyy-MM-dd HH:mm}");
                    }

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Item").SemiBold();
                            header.Cell().AlignRight().Text("Qty").SemiBold();
                            header.Cell().AlignRight().Text("Total").SemiBold();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().Text(item.MenuItem.Name);
                            table.Cell().AlignRight().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text(item.LineTotal.ToString("C"));
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().AlignRight().Text($"Subtotal: {order.Subtotal:C}").SemiBold();
                    column.Item().AlignRight().Text($"Total: {order.TotalAmount:C}").Bold();
                    column.Item().AlignRight().Text($"Paid: {paidTotal:C}");
                    if (balance > 0)
                    {
                        column.Item().AlignRight().Text($"Balance: {balance:C}").FontColor(Colors.Red.Darken2);
                    }
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"receipt-{order.Id}.pdf");
    }

    [HttpPost]
    [Authorize(Roles = Roles.Waiter + "," + Roles.Admin)]
    public async Task<ActionResult<OrderDto>> Create(OrderCreateRequest request)
    {
        var table = await _db.DiningTables.FindAsync(request.DiningTableId);
        if (table == null)
        {
            return BadRequest("Table not found.");
        }

        if (!table.IsAvailable)
        {
            return BadRequest("Table is not available.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("Order must have at least one item.");
        }

        var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems.Where(m => menuItemIds.Contains(m.Id)).ToListAsync();
        if (menuItems.Count != menuItemIds.Count)
        {
            return BadRequest("One or more menu items are invalid.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return Unauthorized();
        }

        var order = new Order
        {
            DiningTableId = table.Id,
            Status = OrderStatus.Open,
            WaiterId = userId
        };

        foreach (var item in request.Items)
        {
            var menuItem = menuItems.First(m => m.Id == item.MenuItemId);
            var lineTotal = menuItem.Price * item.Quantity;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = menuItem.Price,
                LineTotal = lineTotal
            });

            order.Subtotal += lineTotal;
        }

        order.TotalAmount = order.Subtotal;

        _db.Orders.Add(order);
        table.IsAvailable = false;

        await _db.SaveChangesAsync();

        var dto = new OrderDto(
            order.Id,
            order.DiningTableId,
            table.Name,
            order.Status.ToString(),
            order.Subtotal,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.MenuItemId, menuItems.First(m => m.Id == i.MenuItemId).Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList());

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, dto);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Waiter + "," + Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> UpdateStatus(int id, OrderStatusUpdateRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest("Invalid status.");
        }

        var order = await _db.Orders
            .Include(o => o.DiningTable)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (newStatus == OrderStatus.Closed || newStatus == OrderStatus.Cancelled)
        {
            order.ClosedAt = DateTime.UtcNow;
            order.DiningTable.IsAvailable = true;
        }

        if (newStatus == OrderStatus.Served)
        {
            await ApplyInventoryUsage(order);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> AddPayment(int id, PaymentCreateRequest request)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        _db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = request.Amount,
            Method = request.Method.Trim(),
            Reference = request.Reference?.Trim()
        });

        if (order.Status == OrderStatus.Served)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ApplyInventoryUsage(Order order)
    {
        var menuItemIds = order.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var recipes = await _db.MenuItemIngredients
            .Where(r => menuItemIds.Contains(r.MenuItemId))
            .ToListAsync();

        foreach (var item in order.Items)
        {
            var recipeItems = recipes.Where(r => r.MenuItemId == item.MenuItemId);
            foreach (var recipe in recipeItems)
            {
                var ingredient = await _db.Ingredients.FindAsync(recipe.IngredientId);
                if (ingredient == null)
                {
                    continue;
                }

                var usage = recipe.Quantity * item.Quantity;
                ingredient.StockQuantity -= usage;
                _db.IngredientStockAdjustments.Add(new IngredientStockAdjustment
                {
                    IngredientId = ingredient.Id,
                    QuantityChange = -usage,
                    Reason = $"Order #{order.Id} usage"
                });
            }
        }
    }
}
