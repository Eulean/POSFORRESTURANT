using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = Roles.Admin)]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<DailyReportDto>> GetDaily([FromQuery] DateTime? dateUtc)
    {
        var dayStart = (dateUtc ?? DateTime.UtcNow).Date;
        var dayEnd = dayStart.AddDays(1);

        var report = await BuildDailyReport(dayStart, dayEnd);
        return Ok(report);
    }

    [HttpGet("daily/export")]
    public async Task<IActionResult> ExportDaily([FromQuery] DateTime? dateUtc)
    {
        var dayStart = (dateUtc ?? DateTime.UtcNow).Date;
        var dayEnd = dayStart.AddDays(1);
        var report = await BuildDailyReport(dayStart, dayEnd);

        var sb = new StringBuilder();
        sb.AppendLine("Date,OrdersToday,TotalSales,IngredientCost,Profit");
        sb.AppendLine($"{report.DateUtc:yyyy-MM-dd},{report.OrdersToday},{report.TotalSales},{report.IngredientCost},{report.Profit}");
        sb.AppendLine();
        sb.AppendLine("TopItems");
        sb.AppendLine("MenuItemId,Name,Quantity");
        foreach (var item in report.TopItems)
        {
            sb.AppendLine($"{item.MenuItemId},\"{item.Name}\",{item.Quantity}");
        }
        sb.AppendLine();
        sb.AppendLine("RevenueByMethod");
        sb.AppendLine("Method,Total");
        foreach (var item in report.RevenueByMethod)
        {
            sb.AppendLine($"\"{item.Method}\",{item.Total}");
        }
        if (report.BusiestHour != null)
        {
            sb.AppendLine();
            sb.AppendLine("BusiestHour,Orders");
            sb.AppendLine($"{report.BusiestHour.Hour}:00,{report.BusiestHour.Orders}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"daily-report-{report.DateUtc:yyyyMMdd}.csv");
    }

    [HttpPost("close-day")]
    public async Task<ActionResult<DailyCloseSummary>> CloseDay([FromBody] CloseDayRequest request)
    {
        var dayStart = (request.DateUtc ?? DateTime.UtcNow).Date;
        var dayEnd = dayStart.AddDays(1);

        var report = await BuildDailyReport(dayStart, dayEnd);

        var existing = await _db.DailyCloseSummaries.FirstOrDefaultAsync(x => x.DateUtc == dayStart);
        if (existing == null)
        {
            existing = new DailyCloseSummary { DateUtc = dayStart };
            _db.DailyCloseSummaries.Add(existing);
        }

        existing.OrdersToday = report.OrdersToday;
        existing.TotalSales = report.TotalSales;
        existing.IngredientCost = report.IngredientCost;
        existing.Profit = report.Profit;
        existing.TopItemsJson = JsonSerializer.Serialize(report.TopItems);
        existing.RevenueByMethodJson = JsonSerializer.Serialize(report.RevenueByMethod);
        existing.BusiestHour = report.BusiestHour?.Hour;
        existing.BusiestHourOrders = report.BusiestHour?.Orders;
        existing.CreatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpGet("close-day")]
    public async Task<ActionResult<DailyCloseSummary>> GetCloseDay([FromQuery] DateTime? dateUtc)
    {
        var dayStart = (dateUtc ?? DateTime.UtcNow).Date;
        var snapshot = await _db.DailyCloseSummaries.FirstOrDefaultAsync(x => x.DateUtc == dayStart);
        if (snapshot == null)
        {
            return NotFound();
        }

        return Ok(snapshot);
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportDto>> GetMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc)
    {
        var (rangeStart, rangeEnd) = ResolveRange(year, month, startUtc, endUtc);
        var report = await BuildRangeReport(rangeStart, rangeEnd);
        return Ok(report);
    }

    [HttpGet("monthly/export")]
    public async Task<IActionResult> ExportMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc)
    {
        var (rangeStart, rangeEnd) = ResolveRange(year, month, startUtc, endUtc);
        var report = await BuildRangeReport(rangeStart, rangeEnd);

        using var workbook = new XLWorkbook();

        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Monthly Summary";
        summary.Cell(1, 1).Style.Font.SetBold();
        summary.Cell(2, 1).Value = "Range";
        summary.Cell(2, 2).Value = $"{report.StartUtc:yyyy-MM-dd} to {report.EndUtc.AddDays(-1):yyyy-MM-dd}";
        summary.Cell(3, 1).Value = "Orders Total";
        summary.Cell(3, 2).Value = report.OrdersTotal;
        summary.Cell(4, 1).Value = "Total Sales";
        summary.Cell(4, 2).Value = report.TotalSales;
        summary.Cell(4, 2).Style.NumberFormat.Format = "$#,##0.00";
        summary.Cell(5, 1).Value = "Ingredient Cost";
        summary.Cell(5, 2).Value = report.IngredientCost;
        summary.Cell(5, 2).Style.NumberFormat.Format = "$#,##0.00";
        summary.Cell(6, 1).Value = "Profit";
        summary.Cell(6, 2).Value = report.Profit;
        summary.Cell(6, 2).Style.NumberFormat.Format = "$#,##0.00";

        summary.Cell(8, 1).Value = "Top Items";
        summary.Cell(8, 1).Style.Font.SetBold();
        summary.Cell(9, 1).Value = "Menu Item";
        summary.Cell(9, 2).Value = "Quantity";
        summary.Range(9, 1, 9, 2).Style.Font.SetBold();

        var topRow = 10;
        foreach (var item in report.TopItems)
        {
            summary.Cell(topRow, 1).Value = item.Name;
            summary.Cell(topRow, 2).Value = item.Quantity;
            topRow++;
        }

        var revenueStart = topRow + 2;
        summary.Cell(revenueStart, 1).Value = "Revenue by Method";
        summary.Cell(revenueStart, 1).Style.Font.SetBold();
        summary.Cell(revenueStart + 1, 1).Value = "Method";
        summary.Cell(revenueStart + 1, 2).Value = "Total";
        summary.Range(revenueStart + 1, 1, revenueStart + 1, 2).Style.Font.SetBold();

        var revenueRow = revenueStart + 2;
        foreach (var method in report.RevenueByMethod)
        {
            summary.Cell(revenueRow, 1).Value = method.Method;
            summary.Cell(revenueRow, 2).Value = method.Total;
            summary.Cell(revenueRow, 2).Style.NumberFormat.Format = "$#,##0.00";
            revenueRow++;
        }

        summary.Columns().AdjustToContents();

        var daily = workbook.Worksheets.Add("Daily Breakdown");
        daily.Cell(1, 1).Value = "Date";
        daily.Cell(1, 2).Value = "Orders";
        daily.Cell(1, 3).Value = "Total Sales";
        daily.Cell(1, 4).Value = "Ingredient Cost";
        daily.Cell(1, 5).Value = "Profit";
        daily.Range(1, 1, 1, 5).Style.Font.SetBold();

        var dayRow = 2;
        foreach (var day in report.DailyBreakdown)
        {
            daily.Cell(dayRow, 1).Value = day.DateUtc.ToString("yyyy-MM-dd");
            daily.Cell(dayRow, 2).Value = day.Orders;
            daily.Cell(dayRow, 3).Value = day.TotalSales;
            daily.Cell(dayRow, 3).Style.NumberFormat.Format = "$#,##0.00";
            daily.Cell(dayRow, 4).Value = day.IngredientCost;
            daily.Cell(dayRow, 4).Style.NumberFormat.Format = "$#,##0.00";
            daily.Cell(dayRow, 5).Value = day.Profit;
            daily.Cell(dayRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            dayRow++;
        }
        daily.Columns().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        var fileName = $"monthly-report-{report.StartUtc:yyyyMM}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<DailyReportDto> BuildDailyReport(DateTime dayStart, DateTime dayEnd)
    {
        var paymentRows = await _db.Payments
            .Where(p => p.PaidAt >= dayStart && p.PaidAt < dayEnd)
            .Select(p => new { p.OrderId, p.Method, p.Amount, p.PaidAt })
            .ToListAsync();

        var paidOrderIds = paymentRows
            .Select(p => p.OrderId)
            .Distinct()
            .ToList();

        var ordersToday = paidOrderIds.Count;
        var totalSales = paymentRows.Sum(p => p.Amount);

        var revenueByMethod = paymentRows
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Method) ? "Unknown" : p.Method)
            .Select(g => new PaymentMethodDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var topItemRows = paidOrderIds.Count == 0
            ? new List<(int MenuItemId, string Name, int Quantity)>()
            : (await (
                    from oi in _db.OrderItems
                    join m in _db.MenuItems on oi.MenuItemId equals m.Id
                    where paidOrderIds.Contains(oi.OrderId)
                    select new { oi.MenuItemId, m.Name, oi.Quantity }
                )
                .ToListAsync())
                .Select(x => (x.MenuItemId, x.Name, x.Quantity))
                .ToList();

        var topItems = topItemRows
            .GroupBy(x => new { x.MenuItemId, x.Name })
            .Select(g => new TopItemDto(g.Key.MenuItemId, g.Key.Name, g.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToList();

        var ingredientCost = await CalculateIngredientCostAsync(paidOrderIds);
        var profit = totalSales - ingredientCost;

        var orderHours = paidOrderIds.Count == 0
            ? new List<DateTime>()
            : await _db.Orders
                .Where(o => paidOrderIds.Contains(o.Id))
                .Select(o => o.CreatedAt)
                .ToListAsync();

        var busiestHourGroup = orderHours
            .GroupBy(d => d.Hour)
            .Select(g => new HourlyCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Orders)
            .FirstOrDefault();

        return new DailyReportDto(dayStart, ordersToday, totalSales, ingredientCost, profit, topItems, revenueByMethod, busiestHourGroup);
    }

    private async Task<MonthlyReportDto> BuildRangeReport(DateTime rangeStart, DateTime rangeEnd)
    {
        var paymentRows = await _db.Payments
            .Where(p => p.PaidAt >= rangeStart && p.PaidAt < rangeEnd)
            .Select(p => new { p.OrderId, p.Method, p.Amount, p.PaidAt })
            .ToListAsync();

        var paidOrderIds = paymentRows
            .Select(p => p.OrderId)
            .Distinct()
            .ToList();

        var ordersTotal = paidOrderIds.Count;
        var totalSales = paymentRows.Sum(p => p.Amount);

        var revenueByMethod = paymentRows
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Method) ? "Unknown" : p.Method)
            .Select(g => new PaymentMethodDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var topItemRows = paidOrderIds.Count == 0
            ? new List<(int MenuItemId, string Name, int Quantity)>()
            : (await (
                    from oi in _db.OrderItems
                    join m in _db.MenuItems on oi.MenuItemId equals m.Id
                    where paidOrderIds.Contains(oi.OrderId)
                    select new { oi.MenuItemId, m.Name, oi.Quantity }
                )
                .ToListAsync())
                .Select(x => (x.MenuItemId, x.Name, x.Quantity))
                .ToList();

        var topItems = topItemRows
            .GroupBy(x => new { x.MenuItemId, x.Name })
            .Select(g => new TopItemDto(g.Key.MenuItemId, g.Key.Name, g.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .Take(10)
            .ToList();

        var ingredientCostTotal = await CalculateIngredientCostAsync(paidOrderIds);
        var profitTotal = totalSales - ingredientCostTotal;

        var paidOrderDateMap = paymentRows
            .GroupBy(p => p.OrderId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.PaidAt).Date);

        var orderCostMap = await CalculateIngredientCostPerOrderAsync(paidOrderIds);

        var salesByDate = paymentRows
            .GroupBy(p => p.PaidAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var dailyBreakdown = new List<DailyBreakdownDto>();
        for (var day = rangeStart.Date; day < rangeEnd.Date; day = day.AddDays(1))
        {
            var dayOrders = paidOrderDateMap.Count(x => x.Value == day);
            salesByDate.TryGetValue(day, out var daySales);
            var dayIngredientCost = orderCostMap
                .Where(x => paidOrderDateMap.TryGetValue(x.Key, out var paidDate) && paidDate == day)
                .Sum(x => x.Value);
            var dayProfit = daySales - dayIngredientCost;
            dailyBreakdown.Add(new DailyBreakdownDto(day, dayOrders, daySales, dayIngredientCost, dayProfit));
        }

        return new MonthlyReportDto(
            rangeStart.Date,
            rangeEnd.Date,
            ordersTotal,
            totalSales,
            ingredientCostTotal,
            profitTotal,
            topItems,
            revenueByMethod,
            dailyBreakdown
        );
    }

    private async Task<decimal> CalculateIngredientCostAsync(List<int> paidOrderIds)
    {
        if (paidOrderIds.Count == 0)
        {
            return 0m;
        }

        var orderItems = await _db.OrderItems
            .Where(oi => paidOrderIds.Contains(oi.OrderId))
            .Select(oi => new { oi.MenuItemId, oi.Quantity })
            .ToListAsync();

        if (orderItems.Count == 0)
        {
            return 0m;
        }

        var menuItemIds = orderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
        var recipeRows = await _db.MenuItemIngredients
            .Where(mii => menuItemIds.Contains(mii.MenuItemId))
            .Select(mii => new { mii.MenuItemId, mii.Quantity, CostPerUnit = mii.Ingredient.CostPerUnit })
            .ToListAsync();

        var recipeCostByMenuItem = recipeRows
            .GroupBy(r => r.MenuItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Quantity * x.CostPerUnit)
            );

        var totalCost = 0m;
        foreach (var item in orderItems)
        {
            recipeCostByMenuItem.TryGetValue(item.MenuItemId, out var unitCost);
            totalCost += unitCost * item.Quantity;
        }

        return totalCost;
    }

    private async Task<Dictionary<int, decimal>> CalculateIngredientCostPerOrderAsync(List<int> paidOrderIds)
    {
        var result = new Dictionary<int, decimal>();
        if (paidOrderIds.Count == 0)
        {
            return result;
        }

        var orderItems = await _db.OrderItems
            .Where(oi => paidOrderIds.Contains(oi.OrderId))
            .Select(oi => new { oi.OrderId, oi.MenuItemId, oi.Quantity })
            .ToListAsync();

        if (orderItems.Count == 0)
        {
            return result;
        }

        var menuItemIds = orderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
        var recipeRows = await _db.MenuItemIngredients
            .Where(mii => menuItemIds.Contains(mii.MenuItemId))
            .Select(mii => new { mii.MenuItemId, mii.Quantity, CostPerUnit = mii.Ingredient.CostPerUnit })
            .ToListAsync();

        var recipeCostByMenuItem = recipeRows
            .GroupBy(r => r.MenuItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Quantity * x.CostPerUnit)
            );

        foreach (var item in orderItems)
        {
            recipeCostByMenuItem.TryGetValue(item.MenuItemId, out var unitCost);
            if (!result.TryGetValue(item.OrderId, out var current))
            {
                current = 0m;
            }
            result[item.OrderId] = current + (unitCost * item.Quantity);
        }

        return result;
    }

    private static (DateTime Start, DateTime End) ResolveRange(
        int? year,
        int? month,
        DateTime? startUtc,
        DateTime? endUtc)
    {
        if (startUtc.HasValue && endUtc.HasValue)
        {
            var start = startUtc.Value.Date;
            var end = endUtc.Value.Date.AddDays(1);
            if (end <= start)
            {
                end = start.AddDays(1);
            }
            return (start, end);
        }

        var baseDate = DateTime.UtcNow;
        var resolvedYear = year ?? baseDate.Year;
        var resolvedMonth = month ?? baseDate.Month;
        var monthStart = new DateTime(resolvedYear, resolvedMonth, 1);
        var monthEnd = monthStart.AddMonths(1);
        return (monthStart, monthEnd);
    }
}
