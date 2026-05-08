using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace EmlakPortali.Api.Services;

public class ImageValidationService
{
    private readonly ILogger<ImageValidationService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
    private readonly Dictionary<string, byte[]> _fileSignatures = new()
    {
        { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } } // RIFF header
    };
    
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public ImageValidationService(ILogger<ImageValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates file signature (magic bytes) to ensure the file is actually what it claims to be.
    /// </summary>
    public ValidationResult ValidateFileSignature(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ValidationResult.Fail("Dosya boş veya geçersiz.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!_allowedExtensions.Contains(extension))
        {
            return ValidationResult.Fail("Desteklenmeyen dosya formatı. Sadece JPG, PNG ve WebP yükleyebilirsiniz.");
        }

        // Check file signature (magic bytes)
        try
        {
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[12]; // Enough for all signatures
            var bytesRead = ReadFull(stream, headerBytes);
            
            if (bytesRead < 4)
            {
                return ValidationResult.Fail("Dosya başlığı okunamadı. Dosya bozuk olabilir.");
            }

            if (!_fileSignatures.TryGetValue(extension, out var expectedSignature))
            {
                return ValidationResult.Fail("Desteklenmeyen dosya formatı.");
            }

            // For WebP, check RIFF header + WEBP marker
            if (extension == ".webp")
            {
                if (bytesRead < 12)
                {
                    return ValidationResult.Fail("WebP dosyası bozuk veya eksik.");
                }
                
                var riffHeader = new byte[] { 0x52, 0x49, 0x46, 0x46 };
                var webpMarker = new byte[] { 0x57, 0x45, 0x42, 0x50 };
                
                if (!HasSignature(headerBytes, riffHeader, 0) || !HasSignature(headerBytes, webpMarker, 8))
                {
                    return ValidationResult.Fail("Geçersiz WebP dosyası. Dosya bozuk olabilir.");
                }
            }
            else
            {
                if (!HasSignature(headerBytes, expectedSignature, 0))
                {
                    return ValidationResult.Fail("Dosya formatı ile uzantısı uyuşmuyor. Dosya bozuk veya geçersiz.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "File signature validation failed for {FileName}", file.FileName);
            return ValidationResult.Fail("Dosya doğrulanamadı. Dosya bozuk olabilir.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates MIME type from the ContentType property.
    /// </summary>
    public ValidationResult ValidateMimeType(IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(file.ContentType))
        {
            return ValidationResult.Fail("Dosya MIME türü belirlenemedi.");
        }

        if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return ValidationResult.Fail($"Desteklenmeyen MIME türü: {file.ContentType}. Sadece JPEG, PNG ve WebP kabul edilir.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates file size.
    /// </summary>
    public ValidationResult ValidateFileSize(IFormFile file)
    {
        if (file.Length > MaxFileSizeBytes)
        {
            var maxMB = MaxFileSizeBytes / (1024.0 * 1024.0);
            return ValidationResult.Fail($"Dosya boyutu çok büyük. Maksimum {maxMB:F0}MB yükleyebilirsiniz.");
        }

        if (file.Length < 1024) // Less than 1KB is suspicious
        {
            return ValidationResult.Fail("Dosya boyutu çok küçük. Dosya bozuk olabilir.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that the file is actually a valid image by attempting to decode it.
    /// </summary>
    public ValidationResult ValidateImageIntegrity(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            
            // Try to load the image using ImageSharp
            var image = Image.Load(stream);
            
            // Basic validation: check dimensions
            if (image.Width < 1 || image.Height < 1)
            {
                image.Dispose();
                return ValidationResult.Fail("Geçersiz görsel boyutları.");
            }

            if (image.Width > 10000 || image.Height > 10000)
            {
                image.Dispose();
                return ValidationResult.Fail("Görsel boyutları çok büyük. Maksimum 10000x10000 piksel.");
            }

            image.Dispose();
            return ValidationResult.Success();
        }
        catch (UnknownImageFormatException)
        {
            return ValidationResult.Fail("Dosya geçerli bir görsel değil. Dosya bozuk olabilir.");
        }
        catch (ImageFormatException ex)
        {
            _logger.LogWarning(ex, "Image format error for {FileName}", file.FileName);
            return ValidationResult.Fail("Görsel formatı hatalı. Dosya bozuk olabilir.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image integrity validation failed for {FileName}", file.FileName);
            return ValidationResult.Fail("Görsel doğrulanamadı. Dosya bozuk veya geçersiz.");
        }
    }

    /// <summary>
    /// Sanitizes the image by re-encoding it to a standard format.
    /// This removes any potentially malicious content embedded in the file.
    /// </summary>
    public async Task<ImageSanitizationResult> SanitizeImageAsync(IFormFile file, string outputPath, string? targetFormat = null)
    {
        targetFormat ??= "jpeg";
        
        try
        {
            using var inputStream = file.OpenReadStream();
            
            // Load and re-encode the image to strip out any malicious content
            using var image = Image.Load(inputStream);
            
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Save with appropriate encoder settings
            if (targetFormat == "jpeg" || targetFormat == "jpg")
            {
                var encoder = new JpegEncoder
                {
                    Quality = 85 // 85% quality for good balance
                };
                await image.SaveAsync(outputPath, encoder);
            }
            else if (targetFormat == "png")
            {
                await image.SaveAsync(outputPath, new PngEncoder());
            }
            else if (targetFormat == "webp")
            {
                await image.SaveAsync(outputPath, new WebpEncoder());
            }
            else
            {
                // Default to JPEG
                var encoder = new JpegEncoder
                {
                    Quality = 85
                };
                await image.SaveAsync(outputPath, encoder);
            }

            return ImageSanitizationResult.Success(
                Path.GetFileName(outputPath),
                outputPath,
                image.Width,
                image.Height
            );
        }
        catch (UnknownImageFormatException)
        {
            _logger.LogWarning("Image sanitization failed: Unknown format for {FileName}", file.FileName);
            
            // Clean up partial file if exists
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            return ImageSanitizationResult.Fail("Dosya geçerli bir görsel değil. Dosya bozuk olabilir.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image sanitization failed for {FileName}", file.FileName);
            
            // Clean up partial file if exists
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            return ImageSanitizationResult.Fail("Görsel işlenirken hata oluştu. Dosya bozuk olabilir.");
        }
    }

    private static int ReadFull(Stream stream, byte[] buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    private static bool HasSignature(byte[] data, byte[] signature, int offset)
    {
        if (data.Length < offset + signature.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (data[offset + i] != signature[i])
                return false;
        }

        return true;
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string message) => new(false, message);
}

public class ImageSanitizationResult
{
    public bool IsSuccess { get; }
    public string? FileName { get; }
    public string? FilePath { get; }
    public int Width { get; }
    public int Height { get; }
    public string? ErrorMessage { get; }

    private ImageSanitizationResult(bool isSuccess, string? fileName = null, string? filePath = null, int width = 0, int height = 0, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        FileName = fileName;
        FilePath = filePath;
        Width = width;
        Height = height;
        ErrorMessage = errorMessage;
    }

    public static ImageSanitizationResult Success(string fileName, string filePath, int width, int height)
        => new(true, fileName, filePath, width, height);

    public static ImageSanitizationResult Fail(string errorMessage)
        => new(false, errorMessage: errorMessage);
}
