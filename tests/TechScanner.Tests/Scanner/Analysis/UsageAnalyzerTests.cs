using TechScanner.Scanner.Analysis;

namespace TechScanner.Tests.Scanner.Analysis;

public class UsageAnalyzerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly UsageAnalyzer _analyzer = new();

    public UsageAnalyzerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ua_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void IsActiveInCode_PackageImported_ReturnsTrue()
    {
        var file = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(file, "import something from 'axios'\nconsole.log('hi')");

        Assert.True(_analyzer.IsActiveInCode("axios", _tempDir));
    }

    [Fact]
    public void IsActiveInCode_PackageNotImported_ReturnsFalse()
    {
        var file = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(file, "import something from 'react'\n");

        Assert.False(_analyzer.IsActiveInCode("axios", _tempDir));
    }

    [Fact]
    public void IsActiveInCode_RequirePattern_ReturnsTrue()
    {
        var file = Path.Combine(_tempDir, "app.js");
        File.WriteAllText(file, "const express = require('express');\n");

        Assert.True(_analyzer.IsActiveInCode("express", _tempDir));
    }

    [Fact]
    public void IsActiveInCode_CSharpUsing_ReturnsTrue()
    {
        var file = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(file, "using Newtonsoft.Json;\nnamespace Foo {}");

        Assert.True(_analyzer.IsActiveInCode("Newtonsoft.Json", _tempDir));
    }

    [Fact]
    public void IsActiveInCode_EmptyDirectory_ReturnsFalse()
    {
        Assert.False(_analyzer.IsActiveInCode("nonexistent", _tempDir));
    }
}
