using TechScanner.Core.Enums;

namespace TechScanner.Core.Models;

public record TechnologyEnrichment(
    string Name,
    SupportStatus Status,
    DateOnly? LastRelease,
    string? Recommendation,
    string? Category);
