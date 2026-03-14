using TechScanner.Core.Enums;

namespace TechScanner.Scanner.Background;

public record ScanJob(Guid ScanId, SourceType SourceType, string SourceInput, string? GitToken);
