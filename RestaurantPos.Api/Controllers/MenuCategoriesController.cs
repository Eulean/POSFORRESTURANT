using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/menu-categories")]
public class MenuCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheVersionKey = "menu-categories:version";

    public MenuCategoriesController(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuCategoryDto>>> GetAll()
    {
        var version = GetCacheVersion();
        var cacheKey = $"menu-categories:{version}";
        var etag = $"W/\"menu-categories-{version}\"";
        if (Request.Headers.IfNoneMatch.Any(tag => string.Equals(tag, etag, StringComparison.Ordinal)))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        if (_cache.TryGetValue(cacheKey, out List<MenuCategoryDto>? cached))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return Ok(cached);
        }

        var items = await _db.MenuCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new MenuCategoryDto(c.Id, c.Name, c.IsActive, c.SortOrder))
            .ToListAsync();

        _cache.Set(cacheKey, items, TimeSpan.FromSeconds(30));
        Response.Headers[HeaderNames.ETag] = etag;
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MenuCategoryDto>> Create(MenuCategoryCreateRequest request)
    {
        var entity = new MenuCategory
        {
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        _db.MenuCategories.Add(entity);
        await _db.SaveChangesAsync();
        BumpCacheVersion();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new MenuCategoryDto(entity.Id, entity.Name, entity.IsActive, entity.SortOrder));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<MenuCategoryDto>> GetById(int id)
    {
        var entity = await _db.MenuCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        return Ok(new MenuCategoryDto(entity.Id, entity.Name, entity.IsActive, entity.SortOrder));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Update(int id, MenuCategoryCreateRequest request)
    {
        var entity = await _db.MenuCategories.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();
        BumpCacheVersion();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _db.MenuCategories.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _db.MenuCategories.Remove(entity);
        await _db.SaveChangesAsync();
        BumpCacheVersion();
        return NoContent();
    }

    private int GetCacheVersion()
    {
        return _cache.GetOrCreate(CacheVersionKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            return 1;
        });
    }

    private void BumpCacheVersion()
    {
        var current = GetCacheVersion();
        _cache.Set(CacheVersionKey, current + 1, TimeSpan.FromHours(6));
    }
}
