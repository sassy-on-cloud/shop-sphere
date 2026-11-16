using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ShopSphere.Models;

namespace ShopSphere.Services;

public sealed class ImageUploadValidationException(string message) : Exception(message);

public sealed class ProductImageStorageService(
    IWebHostEnvironment environment,
    ILogger<ProductImageStorageService> logger)
{
    public const long MaximumFileSize = 5 * 1024 * 1024;
    private const string PublicDirectory = "/uploads/products/";

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is < 12 or > MaximumFileSize)
        {
            throw new ImageUploadValidationException("Choose an image between 12 bytes and 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            throw new ImageUploadValidationException("Only JPEG, PNG, and WebP images are allowed.");
        }

        await using var source = file.OpenReadStream();
        var header = new byte[12];
        var headerLength = 0;
        while (headerLength < header.Length)
        {
            var read = await source.ReadAsync(header.AsMemory(headerLength), cancellationToken);
            if (read == 0)
            {
                break;
            }

            headerLength += read;
        }

        if (headerLength != header.Length || !HasMatchingSignature(extension, header))
        {
            throw new ImageUploadValidationException(
                "The uploaded file content does not match a supported image format.");
        }

        var storedExtension = extension == ".jpeg" ? ".jpg" : extension;
        var fileName = $"{Guid.NewGuid():N}{storedExtension}";
        var directory = GetUploadDirectory();
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, fileName);
        var completed = false;

        try
        {
            await using var destination = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await destination.WriteAsync(header, cancellationToken);
            long totalBytes = headerLength;
            var buffer = new byte[81920];
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (read > MaximumFileSize - totalBytes)
                {
                    throw new ImageUploadValidationException("Images must be 5 MB or smaller.");
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalBytes += read;
            }

            completed = true;
            return PublicDirectory + fileName;
        }
        finally
        {
            if (!completed)
            {
                DeleteFile(fullPath);
            }
        }
    }

    public Task DeleteManagedImageAsync(string? imageUrl)
    {
        if (TryGetManagedPath(imageUrl, out var fullPath))
        {
            DeleteFile(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetUploadDirectory()
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return Path.GetFullPath(Path.Combine(webRoot, "uploads", "products"));
    }

    private bool TryGetManagedPath(string? imageUrl, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrEmpty(imageUrl) ||
            !imageUrl.StartsWith(PublicDirectory, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = imageUrl[PublicDirectory.Length..];
        var extension = Path.GetExtension(fileName);
        if (fileName != Path.GetFileName(fileName) ||
            extension is not (".jpg" or ".png" or ".webp") ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _))
        {
            return false;
        }

        fullPath = Path.GetFullPath(Path.Combine(GetUploadDirectory(), fileName));
        var directoryPrefix = GetUploadDirectory() + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private void DeleteFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not remove a product image file.");
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "The process was not permitted to remove a product image file.");
        }
    }

    private static bool HasMatchingSignature(string extension, byte[] header)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
            ".webp" => header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
                       header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P',
            _ => false
        };
    }
}
