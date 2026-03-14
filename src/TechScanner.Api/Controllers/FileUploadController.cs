using Microsoft.AspNetCore.Mvc;

namespace TechScanner.Api.Controllers;

[ApiController]
[Route("api/upload")]
public class FileUploadController : ControllerBase
{
    private static readonly byte[] ZipMagicBytes = [0x50, 0x4B, 0x03, 0x04];
    private const long MaxFileSizeBytes = 100L * 1024 * 1024; // 100 MB

    [HttpPost]
    [RequestSizeLimit(104_857_600)] // 100 MB
    public async Task<IActionResult> UploadZipAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 100 MB limit.");

        // Validate extension
        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .zip files are accepted.");

        // Validate magic bytes
        using var headerStream = file.OpenReadStream();
        var header = new byte[4];
        var bytesRead = 0;
        while (bytesRead < 4)
        {
            var n = await headerStream.ReadAsync(header.AsMemory(bytesRead, 4 - bytesRead));
            if (n == 0) break;
            bytesRead += n;
        }
        if (bytesRead < 4 || !header.SequenceEqual(ZipMagicBytes))
            return BadRequest("File does not appear to be a valid ZIP archive.");

        // Sanitize filename — prevent path traversal
        var safeFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Contains(".."))
            return BadRequest("Invalid file name.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"techscanner_upload_{Guid.NewGuid()}.zip");

        await using var stream = System.IO.File.Create(tempPath);
        await file.CopyToAsync(stream);

        return Ok(new { tempFilePath = tempPath });
    }
}
