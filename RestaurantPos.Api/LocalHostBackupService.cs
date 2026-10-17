using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace RestaurantPos.Api;

public sealed class LocalHostBackupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalHostBackupService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalHostBackupService(IConfiguration configuration, ILogger<LocalHostBackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var dataDirectory = _configuration["LocalHost:DataDirectory"]
                ?? throw new InvalidOperationException("Local host data directory is not configured.");
            var backupDirectory = Path.Combine(dataDirectory, "Backups");
            Directory.CreateDirectory(backupDirectory);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(backupDirectory, $"RestaurantPos-{stamp}-{Guid.NewGuid():N}.zip");
            var tempDirectory = Path.Combine(backupDirectory, $".working-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);
            try
            {
                var snapshotPath = Path.Combine(tempDirectory, "restaurantpos.db");
                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Database connection is not configured.");
                var sourceOptions = new SqliteConnectionStringBuilder(connectionString) { Pooling = false };
                var snapshotOptions = new SqliteConnectionStringBuilder { DataSource = snapshotPath, Pooling = false };
                using (var source = new SqliteConnection(sourceOptions.ToString()))
                using (var snapshot = new SqliteConnection(snapshotOptions.ToString()))
                {
                    source.Open();
                    snapshot.Open();
                    source.BackupDatabase(snapshot);
                }

                using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(snapshotPath, "restaurantpos.db");
                    foreach (var name in new[] { "logo.webp", "logo.png" })
                    {
                        var logoPath = Path.Combine(dataDirectory, name);
                        if (File.Exists(logoPath)) { archive.CreateEntryFromFile(logoPath, name); }
                    }
                    var imagesDirectory = Path.Combine(dataDirectory, "images");
                    if (Directory.Exists(imagesDirectory))
                    {
                        foreach (var imagePath in Directory.EnumerateFiles(imagesDirectory, "*", SearchOption.AllDirectories))
                        {
                            archive.CreateEntryFromFile(imagePath, Path.GetRelativePath(dataDirectory, imagePath).Replace('\\', '/'));
                        }
                    }
                }
                return backupPath;
            }
            catch
            {
                if (File.Exists(backupPath)) { File.Delete(backupPath); }
                throw;
            }
            finally
            {
                if (Directory.Exists(tempDirectory)) { Directory.Delete(tempDirectory, recursive: true); }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CreateBackupAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Automatic local-host backup failed.");
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await CreateBackupAsync(stoppingToken); }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Automatic local-host backup failed.");
            }
        }
    }
}
