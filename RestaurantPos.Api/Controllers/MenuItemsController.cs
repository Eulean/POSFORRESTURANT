using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenuItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAll([FromQuery] int? categoryId, [FromQuery] bool? activeOnly)
    {
        var query = _db.MenuItems.AsQueryable();

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

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<MenuItemDto>> GetById(int id)
    {
        var entity = await _db.MenuItems.FindAsync(id);
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

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<MenuItemDto>> UploadImage(int id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Image file is required.");
        }

        var entity = await _db.MenuItems.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("images", "menu", fileName).Replace("\\", "/");
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "menu", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        entity.ImageUrl = $"{Request.Scheme}://{Request.Host}/{relativePath}";
        await _db.SaveChangesAsync();

        return Ok(new MenuItemDto(entity.Id, entity.Name, entity.Description, entity.ImageUrl, entity.Price, entity.IsActive, entity.CategoryId));
    }
}
