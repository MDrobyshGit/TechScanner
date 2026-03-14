using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechScanner.Core.Interfaces;

namespace TechScanner.Infrastructure.Llm;

public class LlmEnricherFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public LlmEnricherFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    public ILlmEnricher Create()
    {
        var provider = _configuration["LlmSettings:Provider"] ?? "OpenAI";
        var apiKey = _configuration["LlmSettings:ApiKey"];
        var model = _configuration["LlmSettings:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new NoOpLlmEnricher();

        return provider switch
        {
            "OpenAI" or "AzureOpenAI" => new OpenAiEnricher(
                apiKey,
                model,
                _loggerFactory.CreateLogger<OpenAiEnricher>()),
            "GitHubModels" => new OpenAiEnricher(
                apiKey,
                model,
                new Uri("https://models.inference.ai.azure.com"),
                _loggerFactory.CreateLogger<OpenAiEnricher>()),
            _ => throw new InvalidOperationException($"Unknown LLM provider: {provider}")
        };
    }
}
