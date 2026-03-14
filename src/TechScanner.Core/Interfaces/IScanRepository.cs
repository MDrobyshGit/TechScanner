using TechScanner.Core.Entities;

namespace TechScanner.Core.Interfaces;

public interface IScanRepository
{
    Task<Scan> CreateAsync(Scan scan);
    Task<Scan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Scan>> GetRecentAsync(int count);
    Task UpdateAsync(Scan scan);
    Task DeleteAsync(Guid id);
}
