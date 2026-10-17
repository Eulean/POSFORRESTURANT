using ImageMagick;

namespace RestaurantPos.Api;

public static class UploadedImageProcessor
{
    public const long MaxUploadBytes = 10 * 1024 * 1024;

    static UploadedImageProcessor()
    {
        ResourceLimits.Width = 12_000;
        ResourceLimits.Height = 12_000;
        ResourceLimits.Area = 40_000_000;
        ResourceLimits.Memory = 128 * 1024 * 1024;
    }

    public static async Task<bool> SaveAsWebPAsync(IFormFile file, string destination, uint maxDimension)
    {
        if (file.Length == 0 || file.Length > MaxUploadBytes)
        {
            return false;
        }

        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("Image destination has no directory.");
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
            if (!supported) { return false; }
            input.Position = 0;
            using var image = new MagickImage(input);
            if (image.Width == 0 || image.Height == 0 ||
                (ulong)image.Width * image.Height > 40_000_000)
            {
                return false;
            }

            image.AutoOrient();
            var scale = Math.Min(1.0, (double)maxDimension / Math.Max(image.Width, image.Height));
            if (scale < 1.0)
            {
                image.Resize(
                    (uint)Math.Max(1, Math.Round(image.Width * scale)),
                    (uint)Math.Max(1, Math.Round(image.Height * scale)));
            }
            image.Strip();
            image.Format = MagickFormat.WebP;
            image.Quality = 82;
            image.Write(tempPath);
            File.Move(tempPath, destination, overwrite: true);
            return true;
        }
        catch (MagickException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(tempPath)) { File.Delete(tempPath); }
        }
    }
}
