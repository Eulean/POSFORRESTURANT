using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/menu-categories")]
public class MenuCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenuCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuCategoryDto>>> GetAll()
    {
        var items = await _db.MenuCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new MenuCategoryDto(c.Id, c.Name, c.IsActive, c.SortOrder))
            .ToListAsync();

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

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new MenuCategoryDto(entity.Id, entity.Name, entity.IsActive, entity.SortOrder));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<MenuCategoryDto>> GetById(int id)
    {
        var entity = await _db.MenuCategories.FindAsync(id);
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
        return NoContent();
    }
}
