using TechScanner.Core.Enums;

namespace TechScanner.Core.Entities;

public class ScanTechnology
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string ManifestFile { get; set; } = string.Empty;
    public bool IsActiveInCode { get; set; }
    public SupportStatus SupportStatus { get; set; } = SupportStatus.Unknown;
    public DateOnly? LastReleaseDate { get; set; }
    public string? Recommendation { get; set; }
    public string? Category { get; set; }
    public string? LlmRawResponse { get; set; }

    public Scan Scan { get; set; } = null!;
}
