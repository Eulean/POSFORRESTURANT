using ImageMagick;

namespace RestaurantPos.Api;

public sealed class KbzPayQrStore
{
    private readonly string _path;

    public KbzPayQrStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var root = configuration["LocalHost:DataDirectory"]
            ?? Path.Combine(environment.ContentRootPath, "wwwroot", "app");
        _path = Path.Combine(root, "kbzpay-qr.png");
    }

    public bool Exists => File.Exists(_path);

    public string PathOnDisk => _path;

    public async Task<bool> SaveAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > UploadedImageProcessor.MaxUploadBytes)
        {
            return false;
        }

        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            using var input = new MemoryStream((int)file.Length);
            await file.CopyToAsync(input);
            var header = input.GetBuffer();
            var supported = input.Length >= 12 && (
                (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4e && header[3] == 0x47) ||
                (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) ||
                (header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
                 header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P'));
            if (!supported) return false;

            input.Position = 0;
            using var image = new MagickImage(input);
            if (image.Width == 0 || image.Height == 0 ||
                (ulong)image.Width * image.Height > 40_000_000)
            {
                return false;
            }

            image.AutoOrient();
            image.Strip();
            // Keep the merchant QR lossless so its modules remain scannable.
            image.Format = MagickFormat.Png;
            image.Write(tempPath);
            File.Move(tempPath, _path, overwrite: true);
            return true;
        }
        catch (MagickException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public void Delete()
    {
        if (Exists) File.Delete(_path);
    }
}
