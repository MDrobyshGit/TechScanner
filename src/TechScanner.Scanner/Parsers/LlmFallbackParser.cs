using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

/// <summary>
/// Fallback parser — marks unrecognized files for LLM processing.
/// </summary>
public class LlmFallbackParser : IManifestParser
{
    public bool CanHandle(string fileName) => true;

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        // Signal to the orchestrator that this file needs LLM parsing
        yield return new RawTechnology("NEEDS_LLM_PARSE", null, filePath);
    }
}
