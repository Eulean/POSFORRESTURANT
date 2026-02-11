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
            .AsNoTracking()
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
            .AsNoTracking()
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
        var shopProfile = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync();
        var shopName = string.IsNullOrWhiteSpace(shopProfile?.Name) ? "GALA taste" : shopProfile.Name;
        var shopAddress = string.IsNullOrWhiteSpace(shopProfile?.Address) ? "123 Main Street, City" : shopProfile.Address;
        var shopPhone = string.IsNullOrWhiteSpace(shopProfile?.Phone) ? "(000) 000-0000" : shopProfile.Phone;

        var logoPath = ResolveReceiptLogoPath();
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A5);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);
                            left.Item().Text(shopName).FontSize(20).Bold();
                            left.Item().Text("Restaurant POS").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken1);
                            left.Item().Text(shopAddress).FontSize(10);
                            left.Item().Text($"Phone: {shopPhone}").FontSize(10);
                        });

                        row.ConstantItem(90).AlignRight().AlignMiddle().Element(logo =>
                        {
                            if (logoPath != null)
                            {
                                logo.Width(80).Image(logoPath).FitWidth();
                            }
                        });
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(info =>
                        {
                            info.Spacing(3);
                            info.Item().Text($"Receipt #{order.Id}").FontSize(12).Bold();
                            info.Item().Text($"Table: {order.DiningTable.Name}");
                            info.Item().Text($"Status: {order.Status}");
                            info.Item().Text($"Opened: {order.CreatedAt:yyyy-MM-dd HH:mm}");
                            if (lastPayment != null)
                            {
                                info.Item().Text($"Last Payment: {lastPayment.PaidAt:yyyy-MM-dd HH:mm}");
                            }
                        });

                        row.ConstantItem(80).AlignRight().AlignBottom().Text(order.CreatedAt.ToString("ddd, MMM dd"))
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

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
                            header.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(6).PaddingLeft(4)
                                .Text("Item").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(6)
                                .AlignRight().Text("Qty").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(6).PaddingRight(4)
                                .AlignRight().Text("Total").SemiBold();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().PaddingVertical(4).Text(item.MenuItem.Name);
                            table.Cell().PaddingVertical(4).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().PaddingVertical(4).AlignRight().Text(FormatMmk(item.LineTotal));
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().AlignRight().Background(Colors.Grey.Lighten4).Padding(10).Column(totals =>
                    {
                        totals.Spacing(4);
                        totals.Item().AlignRight().Text($"Subtotal: {FormatMmk(order.Subtotal)}").SemiBold();
                        totals.Item().AlignRight().Text($"Total: {FormatMmk(order.TotalAmount)}").Bold();
                        totals.Item().AlignRight().Text($"Paid: {FormatMmk(paidTotal)}");
                        if (balance > 0)
                        {
                            totals.Item().AlignRight().Text($"Balance: {FormatMmk(balance)}").FontColor(Colors.Red.Darken2);
                        }
                    });

                    if (balance > 0)
                    {
                        // balance already shown in totals box
                    }

                    column.Item().PaddingTop(6).AlignCenter().Text("Thank you for dining with us!")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    column.Item().AlignCenter().Text("Please come again.").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"receipt-{order.Id}.pdf");
    }

    private static string? ResolveReceiptLogoPath()
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var appLogo = Path.Combine(contentRoot, "wwwroot", "app", "logo.png");
        if (System.IO.File.Exists(appLogo))
        {
            return appLogo;
        }

        var rootLogo = Path.Combine(contentRoot, "wwwroot", "logo.png");
        if (System.IO.File.Exists(rootLogo))
        {
            return rootLogo;
        }

        var devLogo = Path.GetFullPath(Path.Combine(contentRoot, "..", "RestaurantPos.Web", "public", "logo.png"));
        if (System.IO.File.Exists(devLogo))
        {
            return devLogo;
        }

        return null;
    }

    private static string FormatMmk(decimal amount)
    {
        return $"MMK {amount:N2}";
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
