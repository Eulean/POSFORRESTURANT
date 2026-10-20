using System.Text;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using RestaurantPos.Api;
using RestaurantPos.Api.Data;
using RestaurantPos.Api.Models;

if (args.Length == 3 && args[0] == "--restore-backup")
{
    try
    {
        var result = LocalHostRecovery.Restore(args[1], args[2]);
        Console.WriteLine($"Restore complete. Pre-restore safety backup: {result}");
        return;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Restore failed: {exception.Message}");
        Environment.ExitCode = 1;
        return;
    }
}

var builder = WebApplication.CreateBuilder(args);
var localHostMode = builder.Environment.IsEnvironment("LocalHost");
string? localHostDataDirectory = null;
if (localHostMode)
{
    var configPath = Environment.GetEnvironmentVariable("RESTAURANTPOS_CONFIG_PATH");
    if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
    {
        throw new InvalidOperationException("Local host configuration is missing. Start the app with Start Restaurant POS.cmd.");
    }

    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
    localHostDataDirectory = builder.Configuration["LocalHost:DataDirectory"];
    if (string.IsNullOrWhiteSpace(localHostDataDirectory) || !Path.IsPathRooted(localHostDataDirectory))
    {
        throw new InvalidOperationException("LocalHost:DataDirectory must be an absolute path.");
    }
    Directory.CreateDirectory(localHostDataDirectory);
}

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<KbzPayQrStore>();
if (localHostMode)
{
    builder.Services.AddSingleton<LocalHostBackupService>();
    builder.Services.AddHostedService(services => services.GetRequiredService<LocalHostBackupService>());
}
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant POS API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { scheme, Array.Empty<string>() }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=restaurantpos.db";
if (connectionString.Contains("Data Source=restaurantpos.db", StringComparison.OrdinalIgnoreCase))
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "restaurantpos.db");
    connectionString = $"Data Source={dbPath}";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5268", "https://localhost:7180" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-change-me-please";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RestaurantPos";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RestaurantPos";
if (builder.Environment.IsProduction() || localHostMode)
{
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("dev-only-change-me-please", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Jwt:Key must be set in production.");
    }

    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 characters in production.");
    }

    if (!localHostMode && allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:Origins must be set in production.");
    }

    if (!localHostMode && allowedOrigins.Any(origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Cors:Origins must not include localhost in production.");
    }

    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
    {
        throw new InvalidOperationException("DefaultConnection Data Source must be set in production.");
    }

    if (!Path.IsPathRooted(sqliteBuilder.DataSource))
    {
        throw new InvalidOperationException("DefaultConnection Data Source must be an absolute path in production.");
    }
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment() && !localHostMode)
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseRouting();
app.UseCors("DefaultCors");

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/logo", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.Equals("/logo.png", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.Equals("/logo.webp", StringComparison.OrdinalIgnoreCase))
    {
        var appRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app");
        var candidates = new[]
        {
            localHostDataDirectory == null ? null : Path.Combine(localHostDataDirectory, "logo.webp"),
            localHostDataDirectory == null ? null : Path.Combine(localHostDataDirectory, "logo.png"),
            Path.Combine(appRoot, "logo.webp"),
            Path.Combine(appRoot, "logo.png"),
            Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "RestaurantPos.Web", "public", "logo.png"))
        };
        var logoPath = candidates.FirstOrDefault(path => path != null && File.Exists(path));
        if (logoPath != null)
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.ContentType = logoPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                ? "image/webp" : "image/png";
            await context.Response.SendFileAsync(logoPath);
            return;
        }
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

if (localHostDataDirectory != null)
{
    var mediaDirectory = Path.Combine(localHostDataDirectory, "images");
    Directory.CreateDirectory(mediaDirectory);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaDirectory),
        RequestPath = "/images"
    });
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.EndsWith("/logo.png", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-store";
            return;
        }

        if (path.Contains("/images/", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
            return;
        }

        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app")),
    DefaultFileNames = new List<string> { "index.html" }
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app")),
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.EndsWith("/logo.png", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-store";
            return;
        }

        if (path.Contains("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000,immutable";
            return;
        }

        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var shouldApply = context.Request.Method == HttpMethods.Get &&
                      context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    var path = context.Request.Path.Value ?? string.Empty;
    if (shouldApply)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Response.StatusCode == StatusCodes.Status200OK &&
                !context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
            {
                context.Response.Headers.CacheControl = "no-store";
            }
            return Task.CompletedTask;
        });
    }

    await next();
});

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/api/host/info", (IConfiguration configuration) =>
{
    var port = configuration.GetValue<int?>("LocalHost:Port") ?? 5268;
    var activeAddresses = NetworkInterface.GetAllNetworkInterfaces()
        .Where(network => network.OperationalStatus == OperationalStatus.Up)
        .SelectMany(network => network.GetIPProperties().UnicastAddresses)
        .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(address => address.Address.ToString())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var addresses = (Environment.GetEnvironmentVariable("RESTAURANTPOS_LAN_ADDRESSES") ?? "")
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(activeAddresses.Contains)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(address => $"http://{address}:{port}/")
        .ToArray();
    var networkReady = bool.TryParse(Environment.GetEnvironmentVariable("RESTAURANTPOS_LAN_READY"), out var ready) && ready && addresses.Length > 0;
    var warning = addresses.Length == 0 && ready
        ? "The laptop's Wi-Fi address changed. Restart the POS to generate a new QR code."
        : Environment.GetEnvironmentVariable("RESTAURANTPOS_LAN_WARNING") ?? string.Empty;
    var networkNames = (Environment.GetEnvironmentVariable("RESTAURANTPOS_NETWORK_NAMES") ?? "")
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return Results.Ok(new { addresses, networkReady, warning, networkNames });
}).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
if (localHostMode)
{
    app.MapPost("/api/host/backup", async (LocalHostBackupService backup, CancellationToken cancellationToken) =>
    {
        var path = await backup.CreateBackupAsync(cancellationToken);
        return Results.Ok(new { path });
    }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
}
app.MapFallbackToFile("app/index.html");

await EnsureDatabaseMigratedAsync(app.Services);
await SeedRolesAsync(app.Services);
await SeedAdminUserAsync(app.Services, builder.Configuration, localHostMode);
if (app.Environment.IsDevelopment())
{
    await SeedSampleDataAsync(app.Services);
}

await ApplySqlitePragmasAsync(app.Services);

app.Run();

static async Task SeedRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task SeedSampleDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

static async Task EnsureDatabaseMigratedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

static async Task ApplySqlitePragmasAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size=-20000;");
}

static async Task SeedAdminUserAsync(IServiceProvider services, IConfiguration configuration, bool localHostMode)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync(Roles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
    }

    var existing = await userManager.FindByNameAsync("admin");
    if (existing != null)
    {
        return;
    }

    var adminUser = new ApplicationUser
    {
        UserName = "admin",
        DisplayName = "Admin"
    };

    var password = localHostMode
        ? Environment.GetEnvironmentVariable("RESTAURANTPOS_BOOTSTRAP_PASSWORD")
        : configuration["BootstrapAdminPassword"] ?? "admin123";
    if (string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException("The first local-host start requires an admin password.");
    }

    var result = await userManager.CreateAsync(adminUser, password);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException("Could not create the admin user: " + string.Join(", ", result.Errors.Select(error => error.Description)));
    }
    else
    {
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
    }
}
