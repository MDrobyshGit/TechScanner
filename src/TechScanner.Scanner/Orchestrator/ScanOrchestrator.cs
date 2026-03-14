using Microsoft.Extensions.Logging;
using TechScanner.Core.Entities;
using TechScanner.Core.Enums;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;
using TechScanner.Scanner.Analysis;
using TechScanner.Scanner.Sources;

namespace TechScanner.Scanner.Orchestrator;

public class ScanOrchestrator
{
    private readonly SourceProviderFactory _providerFactory;
    private readonly IEnumerable<IManifestParser> _parsers;
    private readonly UsageAnalyzer _usageAnalyzer;
    private readonly ILlmEnricher _llmEnricher;
    private readonly IScanRepository _scanRepository;
    private readonly FileCollector _fileCollector;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(
        SourceProviderFactory providerFactory,
        IEnumerable<IManifestParser> parsers,
        UsageAnalyzer usageAnalyzer,
        ILlmEnricher llmEnricher,
        IScanRepository scanRepository,
        FileCollector fileCollector,
        ILogger<ScanOrchestrator> logger)
    {
        _providerFactory = providerFactory;
        _parsers = parsers;
        _usageAnalyzer = usageAnalyzer;
        _llmEnricher = llmEnricher;
        _scanRepository = scanRepository;
        _fileCollector = fileCollector;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid scanId,
        string? gitToken,
        IProgress<ScanProgress> progress,
        CancellationToken ct = default)
    {
        var scan = await _scanRepository.GetByIdAsync(scanId)
            ?? throw new InvalidOperationException($"Scan {scanId} not found.");

        // Embed git token into source input for GitRepoProvider
        if (!string.IsNullOrWhiteSpace(gitToken) &&
            scan.SourceType == SourceType.GitRepository)
            scan.SourceInput = $"{scan.SourceInput}|{gitToken}";

        scan.Status = ScanStatus.Running;
        await _scanRepository.UpdateAsync(scan);

        var provider = _providerFactory.GetProvider(scan.SourceType);
        string? tempPath = null;

        try
        {
            progress.Report(new ScanProgress(10, "Preparing source..."));
            tempPath = await provider.PrepareAsync(scan.SourceInput, ct);

            progress.Report(new ScanProgress(20, "Collecting manifest files..."));
            var files = _fileCollector.Collect(tempPath).ToList();
            _logger.LogInformation("Found {Count} manifest files to parse", files.Count);

            progress.Report(new ScanProgress(30, "Parsing manifests..."));
            var rawTechnologies = ParseFiles(files).ToList();
            _logger.LogInformation("Extracted {Count} raw technologies", rawTechnologies.Count);

            progress.Report(new ScanProgress(60, "Analyzing code usage..."));
            var usageMap = rawTechnologies
                .Where(t => t.Name != "NEEDS_LLM_PARSE")
                .DistinctBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    t => t.Name,
                    t => _usageAnalyzer.IsActiveInCode(t.Name, tempPath),
                    StringComparer.OrdinalIgnoreCase);

            progress.Report(new ScanProgress(75, "Enriching with LLM..."));
            var enrichments = (await _llmEnricher.EnrichAsync(rawTechnologies, ct))
                .ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

            progress.Report(new ScanProgress(90, "Saving results..."));
            scan.Technologies = rawTechnologies
                .Where(t => t.Name != "NEEDS_LLM_PARSE")
                .GroupBy(t => (t.Name, t.ManifestFile))
                .Select(g =>
                {
                    var raw = g.First();
                    enrichments.TryGetValue(raw.Name, out var enrichment);
                    usageMap.TryGetValue(raw.Name, out var isActive);
                    return new ScanTechnology
                    {
                        Id = Guid.NewGuid(),
                        ScanId = scan.Id,
                        Name = raw.Name,
                        Version = raw.Version,
                        ManifestFile = raw.ManifestFile.Replace(tempPath, string.Empty).TrimStart('/', '\\'),
                        IsActiveInCode = isActive,
                        SupportStatus = enrichment?.Status ?? SupportStatus.Unknown,
                        LastReleaseDate = enrichment?.LastRelease,
                        Recommendation = enrichment?.Recommendation,
                        Category = enrichment?.Category
                    };
                })
                .ToList();

            scan.Status = ScanStatus.Completed;
            scan.CompletedAt = DateTime.UtcNow;
            await _scanRepository.UpdateAsync(scan);
            progress.Report(new ScanProgress(100, "Scan complete."));
        }
        catch (OperationCanceledException)
        {
            scan.Status = ScanStatus.Failed;
            scan.ErrorMessage = "Scan was cancelled.";
            await _scanRepository.UpdateAsync(scan);
            progress.Report(new ScanProgress(100, "Scan cancelled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan {ScanId} failed", scan.Id);
            scan.Status = ScanStatus.Failed;
            scan.ErrorMessage = ex.Message;
            await _scanRepository.UpdateAsync(scan);
            progress.Report(new ScanProgress(100, $"Error: {ex.Message}"));
        }
        finally
        {
            if (tempPath != null)
                provider.Cleanup(tempPath);
        }
    }

    private IEnumerable<RawTechnology> ParseFiles(IReadOnlyList<string> files)
    {
        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            // Find the most specific parser (non-fallback takes priority)
            var parser = _parsers.FirstOrDefault(p =>
                p is not TechScanner.Scanner.Parsers.LlmFallbackParser && p.CanHandle(fileName));

            if (parser == null) continue;

            string content;
            try { content = File.ReadAllText(filePath); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read file {FilePath}", filePath);
                continue;
            }

            IEnumerable<RawTechnology> results;
            try { results = parser.Parse(filePath, content).ToList(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Parser {Parser} failed on {FilePath}", parser.GetType().Name, filePath);
                continue;
            }

            foreach (var tech in results)
                yield return tech;
        }
    }
}
