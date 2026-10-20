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
    private readonly IConfiguration _configuration;
    private readonly KbzPayQrStore _kbzPayQrStore;

    public OrdersController(AppDbContext db, IConfiguration configuration, KbzPayQrStore kbzPayQrStore)
    {
        _db = db;
        _configuration = configuration;
        _kbzPayQrStore = kbzPayQrStore;
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

    private string? ResolveReceiptLogoPath()
    {
        var dataDirectory = _configuration["LocalHost:DataDirectory"];
        if (dataDirectory != null)
        {
            var localWebpLogo = Path.Combine(dataDirectory, "logo.webp");
            if (System.IO.File.Exists(localWebpLogo)) { return localWebpLogo; }
            var localLogo = Path.Combine(dataDirectory, "logo.png");
            if (System.IO.File.Exists(localLogo)) { return localLogo; }
        }
        var contentRoot = Directory.GetCurrentDirectory();
        var appWebpLogo = Path.Combine(contentRoot, "wwwroot", "app", "logo.webp");
        if (System.IO.File.Exists(appWebpLogo)) { return appWebpLogo; }
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

        var canAdvance = (order.Status, newStatus) switch
        {
            (OrderStatus.Open, OrderStatus.InProgress) => true,
            (OrderStatus.InProgress, OrderStatus.Ready) => true,
            (OrderStatus.Ready, OrderStatus.Served) => true,
            (OrderStatus.Open or OrderStatus.InProgress or OrderStatus.Ready or OrderStatus.Served, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Closed) => User.IsInRole(Roles.Cashier) || User.IsInRole(Roles.Admin),
            _ => false
        };
        if (!canAdvance)
        {
            return Conflict("This status change is not allowed. Payments must be confirmed through checkout.");
        }
        if (order.Status == OrderStatus.Paid &&
            !await _db.Payments.AnyAsync(p => p.OrderId == order.Id))
        {
            return Conflict("A payment record is required before closing the order.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var changed = await _db.Orders.Where(o => o.Id == id && o.Status == order.Status)
            .ExecuteUpdateAsync(update => update
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.UpdatedAt, now)
                .SetProperty(o => o.ClosedAt, o => newStatus == OrderStatus.Closed || newStatus == OrderStatus.Cancelled ? now : o.ClosedAt));
        if (changed != 1)
        {
            return Conflict("This order was already changed by another staff member. Refresh and try again.");
        }

        if (newStatus == OrderStatus.Closed || newStatus == OrderStatus.Cancelled)
        {
            order.DiningTable.IsAvailable = true;
        }

        if (newStatus == OrderStatus.Served)
        {
            await ApplyInventoryUsage(order);
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> AddPayment(int id, PaymentCreateRequest request)
    {
        var method = request.Method?.Trim();
        if (method is not ("Cash" or "Card" or "Mobile"))
        {
            return BadRequest("Choose Cash, Card, or Mobile. KBZPay uses its own confirmation flow.");
        }
        if (request.Amount <= 0)
        {
            return BadRequest("Payment amount must be positive.");
        }
        var reference = request.Reference?.Trim();
        if (reference?.Length > 100)
        {
            return BadRequest("Reference must be 100 characters or fewer.");
        }
        return await CompletePaymentAsync(id, OrderStatus.Served, request.Amount, method, reference);
    }

    [HttpPost("{id:int}/kbzpay/start")]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> StartKbzPay(int id)
    {
        if (!_kbzPayQrStore.Exists)
        {
            return Conflict("An admin must upload the restaurant's KBZPay merchant QR first.");
        }
        var changed = await _db.Orders
            .Where(o => o.Id == id && o.Status == OrderStatus.Served &&
                !_db.Payments.Any(p => p.OrderId == o.Id))
            .ExecuteUpdateAsync(update => update
                .SetProperty(o => o.Status, OrderStatus.PaymentPending)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
        if (changed == 0)
        {
            return await _db.Orders.AnyAsync(o => o.Id == id)
                ? Conflict("Only an unpaid served order can start KBZPay checkout.") : NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id:int}/kbzpay/cancel")]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> CancelKbzPay(int id)
    {
        var changed = await _db.Orders
            .Where(o => o.Id == id && o.Status == OrderStatus.PaymentPending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(o => o.Status, OrderStatus.Served)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
        if (changed == 0)
        {
            return await _db.Orders.AnyAsync(o => o.Id == id)
                ? Conflict("This order is not awaiting KBZPay confirmation.") : NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id:int}/kbzpay/confirm")]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public async Task<ActionResult> ConfirmKbzPay(int id, KbzPayConfirmRequest request)
    {
        var reference = request.Reference?.Trim();
        if (!request.VerifiedInPartnerApp || string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
        {
            return BadRequest("Check the payment in KBZPay Partner and enter its transaction reference.");
        }

        var total = await _db.Orders.Where(o => o.Id == id).Select(o => (decimal?)o.TotalAmount).FirstOrDefaultAsync();
        if (total == null) return NotFound();
        return await CompletePaymentAsync(id, OrderStatus.PaymentPending, total.Value, "KBZPay", reference);
    }

    private async Task<ActionResult> CompletePaymentAsync(int id, OrderStatus expectedStatus,
        decimal amount, string method, string? reference)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var order = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new { o.TotalAmount, o.DiningTableId })
            .FirstOrDefaultAsync();
        if (order == null) return NotFound();
        if (amount != order.TotalAmount || order.TotalAmount <= 0)
        {
            return BadRequest("Payment must match the full order total.");
        }
        if (method == "KBZPay" &&
            await _db.Payments.AnyAsync(p => p.Method == "KBZPay" && p.Reference == reference))
        {
            return Conflict("This KBZPay transaction reference was already recorded.");
        }

        var now = DateTime.UtcNow;
        var changed = await _db.Orders
            .Where(o => o.Id == id && o.Status == expectedStatus &&
                !_db.Payments.Any(p => p.OrderId == o.Id))
            .ExecuteUpdateAsync(update => update
                .SetProperty(o => o.Status, OrderStatus.Closed)
                .SetProperty(o => o.UpdatedAt, now)
                .SetProperty(o => o.ClosedAt, now));
        if (changed == 0)
        {
            return Conflict("This order is not awaiting this payment or has already been paid.");
        }

        await _db.DiningTables.Where(t => t.Id == order.DiningTableId)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.IsAvailable, true));
        _db.Payments.Add(new Payment
        {
            OrderId = id,
            Amount = amount,
            Method = method,
            Reference = reference,
            PaidAt = now
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
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
