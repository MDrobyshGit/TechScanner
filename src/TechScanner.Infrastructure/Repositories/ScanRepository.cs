using Microsoft.EntityFrameworkCore;
using TechScanner.Core.Entities;
using TechScanner.Core.Interfaces;
using TechScanner.Infrastructure.Data;

namespace TechScanner.Infrastructure.Repositories;

public class ScanRepository : IScanRepository
{
    private readonly TechScannerDbContext _context;

    public ScanRepository(TechScannerDbContext context)
    {
        _context = context;
    }

    public async Task<Scan> CreateAsync(Scan scan)
    {
        _context.Scans.Add(scan);
        await _context.SaveChangesAsync();
        return scan;
    }

    public async Task<Scan?> GetByIdAsync(Guid id)
    {
        return await _context.Scans
            .Include(s => s.Technologies)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Scan>> GetRecentAsync(int count)
    {
        return await _context.Scans
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task UpdateAsync(Scan scan)
    {
        // Update scan columns without involving the change tracker
        await _context.Scans
            .Where(s => s.Id == scan.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, scan.Status)
                .SetProperty(x => x.CompletedAt, scan.CompletedAt)
                .SetProperty(x => x.ErrorMessage, scan.ErrorMessage)
                .SetProperty(x => x.SourceInput, scan.SourceInput));

        // Insert any new technologies that haven't been persisted yet
        var newTechs = scan.Technologies
            .Where(t => _context.Entry(t).State == EntityState.Detached)
            .ToList();
        if (newTechs.Count > 0)
        {
            await _context.ScanTechnologies.AddRangeAsync(newTechs);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var scan = await _context.Scans.FindAsync(id);
        if (scan != null)
        {
            _context.Scans.Remove(scan);
            await _context.SaveChangesAsync();
        }
    }
}
