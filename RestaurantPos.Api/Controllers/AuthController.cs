using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantPos.Api.DTOs;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var role = request.Role.Trim();
        if (!Roles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Invalid role. Allowed: {string.Join(", ", Roles.All)}");
        }

        var isFirstUser = !await _userManager.Users.AnyAsync();
        if (!isFirstUser)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            if (!User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }
        }

        if (isFirstUser)
        {
            role = Roles.Admin;
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            DisplayName = request.DisplayName?.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        var token = CreateToken(user, roles);
        return Ok(token);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName.Trim());
        if (user == null)
        {
            return Unauthorized();
        }

        var ok = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = CreateToken(user, roles);
        return Ok(token);
    }

    private AuthResponse CreateToken(ApplicationUser user, IList<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "RestaurantPos";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "RestaurantPos";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 480;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(tokenString, expires, user.Id, user.UserName ?? string.Empty, roles.ToArray());
    }
}
