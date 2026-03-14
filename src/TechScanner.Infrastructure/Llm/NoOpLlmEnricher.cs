using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Infrastructure.Llm;

/// <summary>Returned when no LLM API key is configured. Skips enrichment silently.</summary>
public class NoOpLlmEnricher : ILlmEnricher
{
    public Task<IEnumerable<TechnologyEnrichment>> EnrichAsync(
        IEnumerable<RawTechnology> technologies,
        CancellationToken ct = default)
        => Task.FromResult(Enumerable.Empty<TechnologyEnrichment>());
}
