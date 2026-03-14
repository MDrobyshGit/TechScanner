using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using TechScanner.Core.Enums;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Infrastructure.Llm;

public class OpenAiEnricher : ILlmEnricher
{
    private readonly ChatClient _client;
    private readonly ILogger<OpenAiEnricher> _logger;

    public OpenAiEnricher(string apiKey, string model, ILogger<OpenAiEnricher> logger)
    {
        _client = new ChatClient(model, apiKey);
        _logger = logger;
    }

    public OpenAiEnricher(string apiKey, string model, Uri endpoint, ILogger<OpenAiEnricher> logger)
    {
        var options = new OpenAIClientOptions { Endpoint = endpoint };
        _client = new ChatClient(model, new ApiKeyCredential(apiKey), options);
        _logger = logger;
    }

    public async Task<IEnumerable<TechnologyEnrichment>> EnrichAsync(
        IEnumerable<RawTechnology> technologies,
        CancellationToken ct = default)
    {
        var techs = technologies
            .Where(t => t.Name != "NEEDS_LLM_PARSE")
            .DistinctBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (techs.Count == 0) return [];

        var techList = techs.Select(t => new { name = t.Name, version = t.Version });
        var techJson = JsonSerializer.Serialize(techList);

        var prompt = $"""
            You are a software technology analyst. For each technology in the list below, provide enrichment data.
            
            Return ONLY a valid JSON array (no markdown, no explanation) where each element has:
            - "name": exact name from input
            - "supportStatus": one of "Active", "Slowing", "Abandoned", "Unknown"
            - "lastReleaseDate": ISO date string "YYYY-MM-DD" or null
            - "recommendation": short action string or null if all is fine
            - "category": e.g. "Frontend Framework", "Database", "Testing", "UI Library", etc.
            
            Technologies: {techJson}
            """;

        try
        {
            var response = await _client.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                new ChatCompletionOptions { MaxOutputTokenCount = 4096 },
                ct);

            var raw = response.Value.Content[0].Text;
            _logger.LogDebug("LLM response: {Raw}", raw);

            return ParseLlmResponse(raw, techs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM enrichment failed, returning Unknown for all technologies");
            return techs.Select(t => new TechnologyEnrichment(t.Name, SupportStatus.Unknown, null, null, null));
        }
    }

    private static IEnumerable<TechnologyEnrichment> ParseLlmResponse(
        string raw, IReadOnlyList<RawTechnology> fallback)
    {
        try
        {
            // Strip markdown code fences if present
            var json = raw.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")];
            json = json.Trim();

            var items = JsonSerializer.Deserialize<List<LlmTechItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items == null) throw new InvalidOperationException("Null response");

            return items.Select(item =>
            {
                Enum.TryParse<SupportStatus>(item.SupportStatus, true, out var status);
                DateOnly.TryParse(item.LastReleaseDate, out var releaseDate);
                return new TechnologyEnrichment(
                    item.Name ?? string.Empty,
                    status,
                    item.LastReleaseDate != null ? releaseDate : null,
                    item.Recommendation,
                    item.Category);
            });
        }
        catch
        {
            return fallback.Select(t => new TechnologyEnrichment(t.Name, SupportStatus.Unknown, null, null, null));
        }
    }

    private sealed class LlmTechItem
    {
        public string? Name { get; set; }
        public string? SupportStatus { get; set; }
        public string? LastReleaseDate { get; set; }
        public string? Recommendation { get; set; }
        public string? Category { get; set; }
    }
}
