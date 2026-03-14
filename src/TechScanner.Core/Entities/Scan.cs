using TechScanner.Core.Enums;

namespace TechScanner.Core.Entities;

public class Scan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SourceType SourceType { get; set; }
    public string SourceInput { get; set; } = string.Empty;
    public ScanStatus Status { get; set; } = ScanStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<ScanTechnology> Technologies { get; set; } = new List<ScanTechnology>();
}
