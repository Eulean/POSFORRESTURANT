using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/shop-profile")]
[Authorize(Roles = Roles.Admin)]
public class ShopProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ShopProfileController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<ShopProfileDto>> Get()
    {
        var profile = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync();
        if (profile == null)
        {
            profile = new ShopProfile();
            _db.ShopProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        return Ok(ToDto(profile, GetLogoUrl()));
    }

    [HttpPut]
    public async Task<ActionResult<ShopProfileDto>> Update([FromBody] ShopProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Shop name is required.");
        }

        var profile = await _db.ShopProfiles.FirstOrDefaultAsync();
        if (profile == null)
        {
            profile = new ShopProfile();
            _db.ShopProfiles.Add(profile);
        }

        profile.Name = request.Name.Trim();
        profile.Address = request.Address?.Trim() ?? string.Empty;
        profile.Phone = request.Phone?.Trim() ?? string.Empty;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(profile, GetLogoUrl()));
    }

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ShopProfileDto>> UploadLogo([FromForm] IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Logo file is required.");
        }

        if (!string.Equals(file.ContentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Logo must be a PNG file.");
        }

        var appRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "app");
        Directory.CreateDirectory(appRoot);
        var appLogoPath = Path.Combine(appRoot, "logo.png");
        await using (var stream = System.IO.File.Create(appLogoPath))
        {
            await file.CopyToAsync(stream);
        }

        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(wwwroot);
        var rootLogoPath = Path.Combine(wwwroot, "logo.png");
        await using (var stream = System.IO.File.Create(rootLogoPath))
        {
            await file.CopyToAsync(stream);
        }

        var profile = await _db.ShopProfiles.FirstOrDefaultAsync();
        if (profile == null)
        {
            profile = new ShopProfile();
            _db.ShopProfiles.Add(profile);
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToDto(profile, GetLogoUrl()));
    }

    private string? GetLogoUrl()
    {
        var logoPath = Path.Combine(_env.ContentRootPath, "wwwroot", "app", "logo.png");
        if (!System.IO.File.Exists(logoPath))
        {
            logoPath = Path.Combine(_env.ContentRootPath, "wwwroot", "logo.png");
        }

        if (!System.IO.File.Exists(logoPath))
        {
            return null;
        }

        return $"/logo.png?v={DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static ShopProfileDto ToDto(ShopProfile profile, string? logoUrl)
    {
        return new ShopProfileDto(profile.Id, profile.Name, profile.Address, profile.Phone, logoUrl, profile.UpdatedAt);
    }
}
