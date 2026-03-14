using TechScanner.Core.Enums;

namespace TechScanner.Api.DTOs;

public record StartScanRequest(SourceType SourceType, string SourceInput, string? GitToken);
