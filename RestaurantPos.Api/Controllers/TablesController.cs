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
[Route("api/tables")]
public class TablesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheVersionKey = "tables:version";

    public TablesController(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<DiningTableDto>>> GetAll()
    {
        var version = GetCacheVersion();
        var cacheKey = $"tables:{version}";
        var etag = $"W/\"tables-{version}\"";
        if (Request.Headers.IfNoneMatch.Any(tag => string.Equals(tag, etag, StringComparison.Ordinal)))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        if (_cache.TryGetValue(cacheKey, out List<DiningTableDto>? cached))
        {
            Response.Headers[HeaderNames.ETag] = etag;
            return Ok(cached);
        }

        var items = await _db.DiningTables.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new DiningTableDto(t.Id, t.Name, t.Capacity, t.IsAvailable))
            .ToListAsync();

        _cache.Set(cacheKey, items, TimeSpan.FromSeconds(15));
        Response.Headers[HeaderNames.ETag] = etag;
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<DiningTableDto>> GetById(int id)
    {
        var entity = await _db.DiningTables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        return Ok(new DiningTableDto(entity.Id, entity.Name, entity.Capacity, entity.IsAvailable));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<DiningTableDto>> Create(DiningTableCreateRequest request)
    {
        var entity = new DiningTable
        {
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            IsAvailable = request.IsAvailable
        };

        _db.DiningTables.Add(entity);
        await _db.SaveChangesAsync();
        BumpCacheVersion();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new DiningTableDto(entity.Id, entity.Name, entity.Capacity, entity.IsAvailable));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Update(int id, DiningTableCreateRequest request)
    {
        var entity = await _db.DiningTables.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.Name.Trim();
        entity.Capacity = request.Capacity;
        entity.IsAvailable = request.IsAvailable;

        await _db.SaveChangesAsync();
        BumpCacheVersion();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _db.DiningTables.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _db.DiningTables.Remove(entity);
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
