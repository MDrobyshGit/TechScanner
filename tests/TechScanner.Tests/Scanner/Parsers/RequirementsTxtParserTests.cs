using TechScanner.Scanner.Parsers;

namespace TechScanner.Tests.Scanner.Parsers;

public class RequirementsTxtParserTests
{
    private readonly RequirementsTxtParser _parser = new();

    [Fact]
    public void CanHandle_ReturnsTrue_ForRequirementsTxt()
        => Assert.True(_parser.CanHandle("requirements.txt"));

    [Fact]
    public void Parse_ValidContent_ExtractsPackages()
    {
        var content = """
            flask==2.3.0
            requests>=2.28.0
            # this is a comment
            pytest~=7.4.0
            boto3
            """;

        var result = _parser.Parse("requirements.txt", content).ToList();
        Assert.Equal(4, result.Count);
        Assert.Contains(result, t => t.Name == "flask" && t.Version == "2.3.0");
        Assert.Contains(result, t => t.Name == "boto3" && t.Version == null);
    }

    [Fact]
    public void Parse_CommentLines_Skipped()
    {
        var content = "# only comments\n# another comment\n";
        Assert.Empty(_parser.Parse("requirements.txt", content));
    }
}
