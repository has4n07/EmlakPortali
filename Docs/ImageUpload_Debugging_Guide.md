# Comprehensive Debugging Guide: Image Upload Failures

## Table of Contents
1. [Problem Statement](#problem-statement)
2. [Error Classification](#error-classification)
3. [Debugging Workflow](#debugging-workflow)
4. [Common Failure Scenarios](#common-failure-scenarios)
5. [Logging and Diagnostics](#logging-and-diagnostics)
6. [Unit Tests](#unit-tests)

---

## Problem Statement

When a user attempts to upload a corrupted or invalid image file as a profile picture, the application may:
- Crash with unhandled exceptions
- Accept malicious files (security vulnerability)
- Store corrupted files that fail on retrieval
- Provide poor user experience with generic error messages

---

## Error Classification

### 1. File-Level Errors (Detected Before Processing)
| Error Type | Detection Method | Error Message | Severity |
|------------|------------------|---------------|----------|
| Empty file | `file.Length == 0` | "Dosya boş." | Low |
| File too large | `file.Length > MaxFileSize` | "Dosya boyutu çok büyük. Maksimum XMB..." | Low |
| File too small | `file.Length < 1024` | "Dosya boyutu çok küçük. Dosya bozuk olabilir." | Medium |
| Invalid extension | Extension check | "Desteklenmeyen dosya formatı..." | Low |
| Invalid MIME type | `file.ContentType` check | "Desteklenmeyen MIME türü..." | Medium |

### 2. Signature-Level Errors (Magic Bytes Validation)
| Error Type | Detection Method | Error Message | Severity |
|------------|------------------|---------------|----------|
| Mismatched signature | Magic bytes check | "Dosya formatı ile uzantısı uyuşmuyor..." | High |
| Truncated file | Header bytes incomplete | "Dosya başlığı okunamadı..." | High |
| Invalid WebP structure | RIFF + WEBP check | "Geçersiz WebP dosyası..." | High |

### 3. Content-Level Errors (Image Integrity)
| Error Type | Detection Method | Error Message | Severity |
|------------|------------------|---------------|----------|
| Corrupted image | `Image.FromStream()` | "Dosya geçerli bir görsel değil..." | High |
| Invalid dimensions | Width/Height check | "Geçersiz görsel boyutları." | Medium |
| Dimensions too large | Max dimension check | "Görsel boyutları çok büyük..." | Medium |
| Out of memory | `OutOfMemoryException` | "Görsel işlenirken bellek yetersiz..." | Critical |

### 4. Processing Errors (During Sanitization)
| Error Type | Detection Method | Error Message | Severity |
|------------|------------------|---------------|----------|
| Re-encoding failure | `image.Save()` exception | "Görsel işlenirken hata oluştu..." | High |
| Disk full | `IOException` | "Dosya kaydedilemedi..." | Critical |
| Permission denied | `UnauthorizedAccessException` | "Sunucu dosya yazma izni hatası." | Critical |

---

## Debugging Workflow

### Step 1: Enable Detailed Logging

Add the following to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "EmlakPortali.Api.Services.ImageValidationService": "Debug",
      "EmlakPortali.Api.Controllers.ProfileController": "Debug"
    }
  }
}
```

### Step 2: Reproduce the Error

Create test files that simulate various failure scenarios:

```bash
# Create a truncated JPEG (first 100 bytes only)
head -c 100 valid_image.jpg > truncated.jpg

# Create a file with wrong extension
cp valid_image.jpg fake.png

# Create a file with invalid magic bytes
echo "This is not an image" > fake.jpg

# Create a polyglot file (image + script)
cat valid_image.jpg malicious_script.sh > polyglot.jpg
```

### Step 3: Check Server Logs

```powershell
# Check API logs
Get-Content "C:\Users\hesme\OneDrive\Masaüstü\EMLAK PORTALI\Logs\api-log.txt" -Tail 50

# Or check in Visual Studio Output window / Debug Console
```

### Step 4: Validate with Debug Endpoint

Add a temporary debug endpoint to test validation without saving:

```csharp
[HttpPost("debug-validate")]
[AllowAnonymous]
public async Task<IActionResult> DebugValidate(IFormFile file)
{
    var result = new Dictionary<string, object>
    {
        ["fileName"] = file.FileName,
        ["contentType"] = file.ContentType,
        ["length"] = file.Length,
        ["headers"] = file.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value))
    };

    // Check signature
    using var stream = file.OpenReadStream();
    var header = new byte[12];
    var read = stream.Read(header, 0, header.Length);
    result["headerBytes"] = BitConverter.ToString(header, 0, read);
    
    // Try to load as image
    try
    {
        stream.Position = 0;
        using var img = Image.FromStream(stream, true, true);
        result["imageWidth"] = img.Width;
        result["imageHeight"] = img.Height;
        result["imageFormat"] = img.RawFormat.Guid.ToString();
    }
    catch (Exception ex)
    {
        result["imageError"] = ex.Message;
    }

    return Ok(result);
}
```

### Step 5: Client-Side Debugging

Open browser DevTools (F12) and check:

1. **Network Tab**: Check the upload request
   - Request headers (Content-Type, Authorization)
   - Request payload (FormData)
   - Response status and body

2. **Console Tab**: Check for JavaScript errors
   ```javascript
   // Test client-side validation
   const file = document.getElementById('fileInput').files[0];
   ImageValidator.validate(file).then(result => {
       console.log('Validation result:', result);
   });
   ```

---

## Common Failure Scenarios & Solutions

### Scenario 1: "File Corrupted" Error

**Symptoms:**
- Error: "Dosya geçerli bir görsel değil. Dosya bozuk olabilir."
- `Image.FromStream()` throws `ArgumentException`

**Root Cause:**
- File is truncated (incomplete download/upload)
- File header is corrupted
- File is not actually an image

**Debug Steps:**
1. Check file size vs. expected size
2. Inspect file header bytes
3. Try opening file in image editor

**Solution:**
```csharp
// Enhanced validation with partial reading
public ValidationResult ValidateImageIntegrity(IFormFile file)
{
    try
    {
        using var stream = file.OpenReadStream();
        
        // Try to read the entire stream to ensure it's complete
        var buffer = new byte[4096];
        int totalRead = 0;
        while (totalRead < file.Length)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0) break; // End of stream reached prematurely
            totalRead += read;
        }
        
        if (totalRead < file.Length)
        {
            return ValidationResult.Fail("Dosya tam olarak okunamadı. Dosya bozuk olabilir.");
        }
        
        stream.Position = 0;
        using var image = Image.FromStream(stream, true, true);
        // ... rest of validation
    }
    // ...
}
```

### Scenario 2: "Unsupported Format" Error

**Symptoms:**
- Error: "Desteklenmeyen dosya formatı..."
- File has correct extension but wrong content

**Root Cause:**
- File extension doesn't match actual content
- Malicious file disguised as image

**Debug Steps:**
1. Check file signature (magic bytes)
2. Compare extension vs. actual format

**Solution:**
The `ValidateFileSignature()` method in `ImageValidationService` handles this.

### Scenario 3: Silent Security Vulnerability

**Symptoms:**
- No error, but file may contain malicious code
- File passes validation but is actually dangerous

**Root Cause:**
- Only checking file extension, not content
- Not re-encoding the image

**Solution:**
The `SanitizeImageAsync()` method re-encodes the image, stripping any embedded content.

---

## Logging and Diagnostics

### Structured Logging with Serilog (Optional Enhancement)

```csharp
// Install: dotnet add package Serilog.AspNetCore
// Program.cs
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day)
    .ReadFrom.Configuration(ctx.Configuration));
```

### Health Check Endpoint

```csharp
// Program.cs
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");
```

---

## Unit Tests

### Test Project Setup

Create a test project:
```bash
dotnet new xunit -n EmlakPortali.Api.Tests
cd EmlakPortali.Api.Tests
dotnet add reference ../EmlakPortali.Api.csproj
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### ImageValidationService Tests

Create `ImageValidationServiceTests.cs`:

```csharp
using EmlakPortali.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Drawing;
using System.Drawing.Imaging;

namespace EmlakPortali.Api.Tests;

public class ImageValidationServiceTests
{
    private readonly ImageValidationService _service;
    private readonly Mock<ILogger<ImageValidationService>> _loggerMock;

    public ImageValidationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ImageValidationService>>();
        _service = new ImageValidationService(_loggerMock.Object);
    }

    [Fact]
    public void ValidateFileSignature_ValidJpeg_ReturnsSuccess()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var stream = new MemoryStream(content);
        
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.Length).Returns(content.Length);

        // Act
        var result = _service.ValidateFileSignature(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFileSignature_InvalidSignature_ReturnsFail()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG signature
        var stream = new MemoryStream(content);
        
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.Length).Returns(content.Length);

        // Act
        var result = _service.ValidateFileSignature(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("uzantısı uyuşmuyor", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFileSize_TooLarge_ReturnsFail()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var largeSize = 15L * 1024 * 1024; // 15MB
        
        fileMock.Setup(f => f.Length).Returns(largeSize);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        // Act
        var result = _service.ValidateFileSize(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("büyük", result.ErrorMessage);
    }

    [Fact]
    public async Task SanitizeImageAsync_ValidImage_CreatesNewFile()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        using var originalImage = new Bitmap(100, 100);
        using var ms = new MemoryStream();
        originalImage.Save(ms, ImageFormat.Jpeg);
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        var outputPath = Path.GetTempFileName() + ".jpg";

        try
        {
            // Act
            var result = await _service.SanitizeImageAsync(fileMock.Object, outputPath);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(outputPath));
            Assert.True(result.Width > 0);
            Assert.True(result.Height > 0);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
```

---

## Summary Checklist

- [x] **Backend Validation**: Magic bytes, MIME type, file size, image integrity
- [x] **Sanitization**: Re-encoding images to strip malicious content
- [x] **Frontend Validation**: Client-side checks before upload
- [x] **Error Messages**: Specific, user-friendly messages
- [x] **Logging**: Structured logging for debugging
- [x] **Unit Tests**: Test coverage for validation logic

---

## Quick Reference: Error Message Mapping

| User Sees | Technical Cause | HTTP Status |
|-----------|----------------|-------------|
| "Dosya boş veya yüklenemedi." | `file.Length == 0` | 400 |
| "Dosya boyutu çok büyük..." | Exceeds max size | 400 |
| "Dosya boyutu çok küçük..." | Less than 1KB | 400 |
| "Desteklenmeyen dosya formatı..." | Invalid extension | 400 |
| "Desteklenmeyen MIME türü..." | Invalid ContentType | 400 |
| "Dosya formatı ile uzantısı uyuşmuyor..." | Magic bytes mismatch | 400 |
| "Dosya geçerli bir görsel değil..." | `Image.FromStream()` fails | 400 |
| "Görsel işlenirken hata oluştu..." | Re-encoding fails | 500 |
