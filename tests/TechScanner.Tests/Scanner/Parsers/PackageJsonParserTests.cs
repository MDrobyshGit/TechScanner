using TechScanner.Scanner.Parsers;

namespace TechScanner.Tests.Scanner.Parsers;

public class PackageJsonParserTests
{
    private readonly PackageJsonParser _parser = new();

    [Fact]
    public void CanHandle_ValidFileName_ReturnsTrue()
    {
        Assert.True(_parser.CanHandle("package.json"));
        Assert.True(_parser.CanHandle("PACKAGE.JSON"));
    }

    [Fact]
    public void CanHandle_OtherFileName_ReturnsFalse()
    {
        Assert.False(_parser.CanHandle("package-lock.json"));
        Assert.False(_parser.CanHandle("project.json"));
    }

    [Fact]
    public void Parse_ValidContent_ExtractsCorrectPackages()
    {
        var content = """
            {
              "dependencies": {
                "react": "^18.2.0",
                "axios": "1.4.0"
              },
              "devDependencies": {
                "vite": "~4.3.0"
              }
            }
            """;

        var result = _parser.Parse("/path/package.json", content).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Name == "react" && t.Version == "18.2.0");
        Assert.Contains(result, t => t.Name == "axios" && t.Version == "1.4.0");
        Assert.Contains(result, t => t.Name == "vite" && t.Version == "4.3.0");
    }

    [Fact]
    public void Parse_EmptyDependencies_ReturnsEmpty()
    {
        var content = """{ "name": "test", "version": "1.0.0" }""";
        var result = _parser.Parse("/path/package.json", content).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MalformedContent_DoesNotThrow()
    {
        var result = _parser.Parse("/path/package.json", "{ invalid json >>>").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(_parser.Parse("/path/package.json", ""));
    }
}
