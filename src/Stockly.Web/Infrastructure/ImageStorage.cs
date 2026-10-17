namespace Stockly.Web.Infrastructure;

/// <summary>Stores product photos under wwwroot/uploads/products with random file names.</summary>
public sealed class ImageStorage(IWebHostEnvironment environment)
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > MaxBytes)
            return "Görsel en fazla 2 MB olabilir.";

        if (!AllowedTypes.ContainsKey(file.ContentType))
            return "Yalnızca JPG, PNG veya WEBP görsel yüklenebilir.";

        return null;
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var folder = Path.Combine(environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():n}{AllowedTypes[file.ContentType]}";
        await using var stream = File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }
}
