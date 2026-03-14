using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TechScanner.Core.Interfaces;

namespace TechScanner.Scanner.Sources;

public class GitRepoProvider : ISourceProvider
{
    private readonly ILogger<GitRepoProvider> _logger;

    public GitRepoProvider(ILogger<GitRepoProvider> logger)
    {
        _logger = logger;
    }

    public async Task<string> PrepareAsync(string input, CancellationToken ct = default)
    {
        // input may be "url" or "url|token" - token injected server-side, never from URL
        var parts = input.Split('|', 2);
        var repoUrl = parts[0].Trim();
        var token = parts.Length > 1 ? parts[1] : null;

        // Validate URL scheme (prevent SSRF — only allow http/https)
        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new ArgumentException("Invalid repository URL. Only http/https URLs are supported.");
        }

        // Build clone URL with token embedded (never logged)
        string cloneUrl;
        if (!string.IsNullOrWhiteSpace(token))
        {
            cloneUrl = $"{uri.Scheme}://oauth2:{token}@{uri.Host}{uri.PathAndQuery}";
        }
        else
        {
            cloneUrl = repoUrl;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"techscanner_{Guid.NewGuid()}");

        _logger.LogInformation("Cloning repository to {TempDir}", tempDir);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        var psi = new ProcessStartInfo("git", $"clone --depth 1 \"{cloneUrl}\" \"{tempDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.WaitForExitAsync(cts.Token);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            // Sanitize error: strip any token from error messages
            var sanitizedError = token != null ? error.Replace(token, "***") : error;
            Cleanup(tempDir);
            throw new InvalidOperationException($"git clone failed: {sanitizedError}");
        }

        return tempDir;
    }

    public void Cleanup(string tempPath)
    {
        if (Directory.Exists(tempPath))
        {
            try
            {
                Directory.Delete(tempPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempPath);
            }
        }
    }
}
