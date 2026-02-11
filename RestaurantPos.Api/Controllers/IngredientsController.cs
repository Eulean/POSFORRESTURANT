using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/ingredients")]
public class IngredientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public IngredientsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<IngredientDto>>> GetAll([FromQuery] bool? activeOnly)
    {
        var query = _db.Ingredients.AsNoTracking().AsQueryable();
        if (activeOnly == true)
        {
            query = query.Where(i => i.IsActive);
        }

        var items = await query
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto(i.Id, i.Name, i.Unit, i.StockQuantity, i.ReorderLevel, i.CostPerUnit, i.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<IngredientDto>> GetById(int id)
    {
        var entity = await _db.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        return Ok(new IngredientDto(entity.Id, entity.Name, entity.Unit, entity.StockQuantity, entity.ReorderLevel, entity.CostPerUnit, entity.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IngredientDto>> Create(IngredientCreateRequest request)
    {
        var entity = new Ingredient
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            CostPerUnit = request.CostPerUnit,
            IsActive = request.IsActive
        };

        _db.Ingredients.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new IngredientDto(entity.Id, entity.Name, entity.Unit, entity.StockQuantity, entity.ReorderLevel, entity.CostPerUnit, entity.IsActive));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Update(int id, IngredientCreateRequest request)
    {
        var entity = await _db.Ingredients.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.Name.Trim();
        entity.Unit = request.Unit.Trim();
        entity.StockQuantity = request.StockQuantity;
        entity.ReorderLevel = request.ReorderLevel;
        entity.CostPerUnit = request.CostPerUnit;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _db.Ingredients.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _db.Ingredients.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
