using TechScanner.Core.Models;

namespace TechScanner.Core.Interfaces;

public interface ILlmEnricher
{
    Task<IEnumerable<TechnologyEnrichment>> EnrichAsync(
        IEnumerable<RawTechnology> technologies,
        CancellationToken ct = default);
}
