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
[Route("api/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private const string CacheVersionKey = "menu-items:version";

    public MenuItemsController(AppDbContext db, IMemoryCache cache, IConfiguration configuration)
    {
        _db = db;
        _cache = cache;
        _configuration = configuration;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAll([FromQuery] int? categoryId, [FromQuery] bool? activeOnly)
    {
        var version = GetCacheVersion();
        var cacheKey = $"menu-items:{version}:cat:{categoryId?.ToString() ?? "all"}:active:{activeOnly?.ToString() ?? "all"}";
        var etag = $"W/\"menu-{version}-{categoryId?.ToString() ?? "all"}-{activeOnly?.ToString() ?? "all"}\"";
        if (Request.Headers.IfNoneMatch.Any(tag => string.Equals(tag, etag, StringComparison.Ordinal)))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        if (_cache.TryGetValue(cacheKey, out List<MenuItemDto>? cached))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return Ok(cached);
        }

        var query = _db.MenuItems.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId);
        }

        if (activeOnly == true)
        {
            query = query.Where(m => m.IsActive);
        }

        var items = await query
            .OrderBy(m => m.Name)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.ImageUrl, m.Price, m.IsActive, m.CategoryId))
            .ToListAsync();

        _cache.Set(cacheKey, items, TimeSpan.FromSeconds(30));
        Response.Headers[HeaderNames.ETag] = etag;
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<MenuItemDto>> GetById(int id)
    {
        var entity = await _db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        return Ok(new MenuItemDto(entity.Id, entity.Name, entity.Description, entity.ImageUrl, entity.Price, entity.IsActive, entity.CategoryId));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MenuItemDto>> Create(MenuItemCreateRequest request)
    {
        var entity = new MenuItem
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            Price = request.Price,
            IsActive = request.IsActive,
            CategoryId = request.CategoryId
        };

        _db.MenuItems.Add(entity);
        await _db.SaveChangesAsync();
        BumpCacheVersion();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new MenuItemDto(entity.Id, entity.Name, entity.Description, entity.ImageUrl, entity.Price, entity.IsActive, entity.CategoryId));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Update(int id, MenuItemCreateRequest request)
    {
        var entity = await _db.MenuItems.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.ImageUrl = request.ImageUrl?.Trim();
        entity.Price = request.Price;
        entity.IsActive = request.IsActive;
        entity.CategoryId = request.CategoryId;

        await _db.SaveChangesAsync();
        BumpCacheVersion();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _db.MenuItems.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _db.MenuItems.Remove(entity);
        await _db.SaveChangesAsync();
        BumpCacheVersion();
        return NoContent();
    }

    [HttpGet("{id:int}/recipe")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> GetRecipe(int id)
    {
        var items = await _db.MenuItemIngredients
            .Where(x => x.MenuItemId == id)
            .Include(x => x.Ingredient)
            .Select(x => new MenuItemIngredientDto(x.Id, x.IngredientId, x.Ingredient.Name, x.Quantity))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id:int}/recipe")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> SetRecipe(int id, List<MenuItemIngredientRequest> items)
    {
        var menuItem = await _db.MenuItems.FindAsync(id);
        if (menuItem == null)
        {
            return NotFound();
        }

        var existing = await _db.MenuItemIngredients.Where(x => x.MenuItemId == id).ToListAsync();
        _db.MenuItemIngredients.RemoveRange(existing);

        foreach (var item in items)
        {
            _db.MenuItemIngredients.Add(new MenuItemIngredient
            {
                MenuItemId = id,
                IngredientId = item.IngredientId,
                Quantity = item.Quantity
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    public sealed class UploadMenuItemImageRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = null!;
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<MenuItemDto>> UploadImage(int id, [FromForm] UploadMenuItemImageRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Image file is required.");
        }

        var entity = await _db.MenuItems.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        var fileName = $"{Guid.NewGuid():N}.webp";
        var relativePath = Path.Combine("images", "menu", fileName).Replace("\\", "/");
        var dataDirectory = _configuration["LocalHost:DataDirectory"];
        var absolutePath = dataDirectory == null
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "menu", fileName)
            : Path.Combine(dataDirectory, "images", "menu", fileName);

        if (!await UploadedImageProcessor.SaveAsWebPAsync(request.File, absolutePath, 1600))
        {
            return BadRequest("Upload a JPEG, PNG, or WebP image smaller than 10 MB and 40 megapixels.");
        }

        entity.ImageUrl = $"/{relativePath}";
        await _db.SaveChangesAsync();
        BumpCacheVersion();

        return Ok(new MenuItemDto(entity.Id, entity.Name, entity.Description, entity.ImageUrl, entity.Price, entity.IsActive, entity.CategoryId));
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
