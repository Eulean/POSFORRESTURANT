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
    private readonly IConfiguration _configuration;

    public ShopProfileController(AppDbContext db, IWebHostEnvironment env, IConfiguration configuration)
    {
        _db = db;
        _env = env;
        _configuration = configuration;
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
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ShopProfileDto>> UploadLogo([FromForm] IFormFile file)
    {
        var dataDirectory = _configuration["LocalHost:DataDirectory"];
        var logoRoot = dataDirectory ?? Path.Combine(_env.ContentRootPath, "wwwroot", "app");
        var logoPath = Path.Combine(logoRoot, "logo.webp");
        if (!await UploadedImageProcessor.SaveAsWebPAsync(file, logoPath, 512))
        {
            return BadRequest("Upload a JPEG, PNG, or WebP image smaller than 10 MB and 40 megapixels.");
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
        var dataDirectory = _configuration["LocalHost:DataDirectory"];
        if (dataDirectory != null &&
            (System.IO.File.Exists(Path.Combine(dataDirectory, "logo.webp")) ||
             System.IO.File.Exists(Path.Combine(dataDirectory, "logo.png"))))
        {
            return $"/logo?v={DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        var appRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "app");
        if (System.IO.File.Exists(Path.Combine(appRoot, "logo.webp")) ||
            System.IO.File.Exists(Path.Combine(appRoot, "logo.png")))
        {
            return $"/logo?v={DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        return null;
    }

    private static ShopProfileDto ToDto(ShopProfile profile, string? logoUrl)
    {
        return new ShopProfileDto(profile.Id, profile.Name, profile.Address, profile.Phone, logoUrl, profile.UpdatedAt);
    }
}
