using TechScanner.Core.Entities;
using TechScanner.Core.Enums;

namespace TechScanner.Api.DTOs;

public record ScanResultDto(
    Guid Id,
    string Status,
    string SourceType,
    string SourceInput,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    IEnumerable<ScanTechnologyDto> Technologies)
{
    public static ScanResultDto FromEntity(Scan scan) => new(
        scan.Id,
        scan.Status.ToString(),
        scan.SourceType.ToString(),
        scan.SourceInput,
        scan.CreatedAt,
        scan.CompletedAt,
        scan.ErrorMessage,
        scan.Technologies.Select(ScanTechnologyDto.FromEntity));
}

public record ScanTechnologyDto(
    Guid Id,
    string Name,
    string? Version,
    string ManifestFile,
    bool IsActiveInCode,
    string SupportStatus,
    string? LastReleaseDate,
    string? Recommendation,
    string? Category)
{
    public static ScanTechnologyDto FromEntity(ScanTechnology t) => new(
        t.Id,
        t.Name,
        t.Version,
        t.ManifestFile,
        t.IsActiveInCode,
        t.SupportStatus.ToString(),
        t.LastReleaseDate?.ToString("yyyy-MM-dd"),
        t.Recommendation,
        t.Category);
}

public record ScanSummaryDto(
    Guid Id,
    string Status,
    string SourceType,
    string SourceInput,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int TechnologyCount,
    int RecommendationCount)
{
    public static ScanSummaryDto FromEntity(Scan scan) => new(
        scan.Id,
        scan.Status.ToString(),
        scan.SourceType.ToString(),
        scan.SourceInput,
        scan.CreatedAt,
        scan.CompletedAt,
        scan.Technologies.Count,
        scan.Technologies.Count(t => !string.IsNullOrWhiteSpace(t.Recommendation)));
}
