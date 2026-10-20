using System.Diagnostics;
using System.IO.Compression;
using System.Net.NetworkInformation;
using Microsoft.Data.Sqlite;

namespace RestaurantPos.Api;

public static class LocalHostRecovery
{
    private static readonly string[] DataFiles = ["restaurantpos.db", "logo.webp", "logo.png", "kbzpay-qr.png", "images",
        "restaurantpos.db-wal", "restaurantpos.db-shm", "restaurantpos.db-journal"];
    private const long MaximumUncompressedBytes = 2L * 1024 * 1024 * 1024;

    public static string Restore(string zipPath, string dataDirectory)
    {
        zipPath = Path.GetFullPath(zipPath);
        dataDirectory = Path.GetFullPath(dataDirectory);
        if (!File.Exists(zipPath)) throw new InvalidOperationException("The selected backup ZIP does not exist.");
        if (!Directory.Exists(dataDirectory) ||
            !Path.GetFileName(dataDirectory).Equals("RestaurantPos", StringComparison.OrdinalIgnoreCase) ||
            Path.GetDirectoryName(dataDirectory) == Path.GetPathRoot(dataDirectory))
            throw new InvalidOperationException("Choose the existing RestaurantPos data folder, not a drive or parent folder.");

        using var current = Process.GetCurrentProcess();
        var currentPath = current.MainModule?.FileName;
        var localDataDirectory = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestaurantPos"));
        if (dataDirectory.Equals(localDataDirectory, StringComparison.OrdinalIgnoreCase) &&
            IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == 5268))
            throw new InvalidOperationException("Port 5268 is in use. Stop Restaurant POS before restoring.");
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id) continue;
                try
                {
                    if (!current.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFullPath(process.MainModule?.FileName ?? "")
                        .Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Restaurant POS is still running. Use Stop Restaurant POS.cmd first.");
                }
                catch (System.ComponentModel.Win32Exception) { /* An unrelated inaccessible process. */ }
            }
        }

        var staging = Path.Combine(dataDirectory, ".restore-staging-" + Guid.NewGuid().ToString("N"));
        var rollback = Path.Combine(dataDirectory, ".restore-rollback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            ExtractValidated(zipPath, staging);
            ValidateDatabase(Path.Combine(staging, "restaurantpos.db"));

            var backupDirectory = Path.Combine(dataDirectory, "Backups");
            Directory.CreateDirectory(backupDirectory);
            var safetyPath = Path.Combine(backupDirectory,
                $"RestaurantPos-before-restore-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip");
            if (File.Exists(Path.Combine(dataDirectory, "restaurantpos.db")))
                CreateSafetyBackup(dataDirectory, safetyPath);

            Directory.CreateDirectory(rollback);
            var movedOld = new List<string>();
            var movedNew = new List<string>();
            try
            {
                foreach (var name in DataFiles)
                {
                    var live = Path.Combine(dataDirectory, name);
                    var old = Path.Combine(rollback, name);
                    if (File.Exists(live)) { File.Move(live, old); movedOld.Add(name); }
                    else if (Directory.Exists(live)) { Directory.Move(live, old); movedOld.Add(name); }
                }
                foreach (var name in DataFiles)
                {
                    var source = Path.Combine(staging, name);
                    var live = Path.Combine(dataDirectory, name);
                    if (File.Exists(source)) { File.Move(source, live); movedNew.Add(name); }
                    else if (Directory.Exists(source)) { Directory.Move(source, live); movedNew.Add(name); }
                }
            }
            catch
            {
                foreach (var name in movedNew)
                {
                    var live = Path.Combine(dataDirectory, name);
                    if (File.Exists(live)) File.Delete(live);
                    else if (Directory.Exists(live)) Directory.Delete(live, recursive: true);
                }
                foreach (var name in movedOld)
                {
                    var old = Path.Combine(rollback, name);
                    var live = Path.Combine(dataDirectory, name);
                    if (File.Exists(old)) File.Move(old, live);
                    else if (Directory.Exists(old)) Directory.Move(old, live);
                }
                throw;
            }

            try { Directory.Delete(rollback, recursive: true); }
            catch (IOException) { Console.Error.WriteLine("Warning: old files remain in " + rollback); }
            return File.Exists(safetyPath) ? safetyPath : "No previous database was present";
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static void ExtractValidated(string zipPath, string staging)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count > 10_000) throw new InvalidOperationException("Backup has too many files.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/')) continue;
            var allowed = DataFiles[..4].Contains(name, StringComparer.OrdinalIgnoreCase) ||
                (name.StartsWith("images/", StringComparison.OrdinalIgnoreCase) &&
                 (name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                  name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                  name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                  name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)));
            var parts = name.Split('/');
            if (!allowed || parts.Any(part => part is "" or "." or ".." || part.Contains(':')) ||
                !names.Add(name) || (entry.ExternalAttributes >> 16 & 0xF000) == 0xA000)
                throw new InvalidOperationException("Backup contains an unsafe or unexpected file: " + name);
            total = checked(total + entry.Length);
            if (total > MaximumUncompressedBytes) throw new InvalidOperationException("Backup is too large to restore safely.");
        }
        if (!names.Contains("restaurantpos.db")) throw new InvalidOperationException("Backup has no restaurantpos.db file.");
        foreach (var entry in archive.Entries.Where(entry => !entry.FullName.EndsWith('/')))
        {
            var name = entry.FullName.Replace('\\', '/');
            var target = Path.GetFullPath(Path.Combine(staging, name.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(staging + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Backup contains a path outside the staging folder.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target);
        }
    }

    private static void ValidateDatabase(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false
        }.ToString());
        connection.Open();
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA integrity_check;";
        if (!string.Equals(check.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The backup database failed SQLite integrity check.");
        foreach (var table in new[] { "Orders", "MenuItems", "AspNetUsers", "__EFMigrationsHistory" })
        {
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=$name";
            query.Parameters.AddWithValue("$name", table);
            if (Convert.ToInt32(query.ExecuteScalar()) != 1)
                throw new InvalidOperationException("The selected ZIP is not a compatible Restaurant POS backup.");
        }
    }

    private static void CreateSafetyBackup(string dataDirectory, string destination)
    {
        var snapshot = Path.Combine(Path.GetDirectoryName(destination)!, ".snapshot-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
                   { DataSource = Path.Combine(dataDirectory, "restaurantpos.db"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            using (var target = new SqliteConnection(new SqliteConnectionStringBuilder
                   { DataSource = snapshot, Pooling = false }.ToString()))
            {
                source.Open();
                target.Open();
                source.BackupDatabase(target);
            }
            using var archive = ZipFile.Open(destination, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(snapshot, "restaurantpos.db");
            foreach (var name in DataFiles.Skip(1).Take(3))
            {
                var source = Path.Combine(dataDirectory, name);
                if (File.Exists(source)) archive.CreateEntryFromFile(source, name);
            }
            var images = Path.Combine(dataDirectory, "images");
            if (Directory.Exists(images))
                foreach (var file in Directory.EnumerateFiles(images, "*", SearchOption.AllDirectories))
                    archive.CreateEntryFromFile(file, Path.GetRelativePath(dataDirectory, file).Replace('\\', '/'));
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            throw;
        }
        finally
        {
            if (File.Exists(snapshot)) File.Delete(snapshot);
        }
    }
}
