using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/tables")]
public class TablesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TablesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<DiningTableDto>>> GetAll()
    {
        var items = await _db.DiningTables
            .OrderBy(t => t.Name)
            .Select(t => new DiningTableDto(t.Id, t.Name, t.Capacity, t.IsAvailable))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<DiningTableDto>> GetById(int id)
    {
        var entity = await _db.DiningTables.FindAsync(id);
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
        return NoContent();
    }
}
