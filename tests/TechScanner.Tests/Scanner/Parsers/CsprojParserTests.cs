using TechScanner.Scanner.Parsers;

namespace TechScanner.Tests.Scanner.Parsers;

public class CsprojParserTests
{
    private readonly CsprojParser _parser = new();

    [Theory]
    [InlineData("MyProject.csproj")]
    [InlineData("packages.config")]
    public void CanHandle_ValidFileNames_ReturnsTrue(string fileName)
        => Assert.True(_parser.CanHandle(fileName));

    [Fact]
    public void CanHandle_OtherFile_ReturnsFalse()
        => Assert.False(_parser.CanHandle("project.json"));

    [Fact]
    public void Parse_SdkStyle_ExtractsPackages()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
                <PackageReference Include="Moq" Version="4.18.4" />
              </ItemGroup>
            </Project>
            """;

        var result = _parser.Parse("proj.csproj", content).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Name == "Newtonsoft.Json" && t.Version == "13.0.1");
    }

    [Fact]
    public void Parse_PackagesConfig_ExtractsPackages()
    {
        var content = """
            <?xml version="1.0" encoding="utf-8"?>
            <packages>
              <package id="EntityFramework" version="6.4.4" targetFramework="net472" />
            </packages>
            """;

        var result = _parser.Parse("packages.config", content).ToList();
        Assert.Single(result);
        Assert.Equal("EntityFramework", result[0].Name);
        Assert.Equal("6.4.4", result[0].Version);
    }

    [Fact]
    public void Parse_MalformedXml_DoesNotThrow()
    {
        var result = _parser.Parse("proj.csproj", "<not valid xml>>>").ToList();
        Assert.Empty(result);
    }
}
