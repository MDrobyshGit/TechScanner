using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechScanner.Core.Entities;
using TechScanner.Core.Enums;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;
using TechScanner.Scanner;
using TechScanner.Scanner.Analysis;
using TechScanner.Scanner.Orchestrator;
using TechScanner.Scanner.Parsers;
using TechScanner.Scanner.Sources;

namespace TechScanner.Tests.Scanner.Orchestrator;

public class ScanOrchestratorTests : IDisposable
{
    private readonly string _tempDir;

    public ScanOrchestratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"orch_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private (ScanOrchestrator orchestrator, Mock<IScanRepository> repoMock, Scan scan) BuildOrchestrator(
        string sourceInput,
        SourceType sourceType = SourceType.LocalFolder,
        ILlmEnricher? enricher = null)
    {
        var scan = new Scan { Id = Guid.NewGuid(), SourceType = sourceType, SourceInput = sourceInput };

        var repoMock = new Mock<IScanRepository>();
        repoMock.Setup(r => r.GetByIdAsync(scan.Id)).ReturnsAsync(scan);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Scan>())).Returns(Task.CompletedTask);

        var providerFactory = new SourceProviderFactory(
            new LocalFolderProvider(),
            new ZipArchiveProvider(),
            new GitRepoProvider(NullLogger<GitRepoProvider>.Instance));

        var parsers = new List<IManifestParser> { new PackageJsonParser() };
        var fileCollector = new FileCollector(parsers);
        var usageAnalyzer = new UsageAnalyzer();

        var llmMock = new Mock<ILlmEnricher>();
        llmMock.Setup(e => e.EnrichAsync(It.IsAny<IEnumerable<RawTechnology>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<TechnologyEnrichment>());
        var llmEnricher = enricher ?? llmMock.Object;

        var orchestrator = new ScanOrchestrator(
            providerFactory,
            parsers,
            usageAnalyzer,
            llmEnricher,
            repoMock.Object,
            fileCollector,
            NullLogger<ScanOrchestrator>.Instance);

        return (orchestrator, repoMock, scan);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulScan_SavesCompletedStatus()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"),
            """{"dependencies":{"react":"18.2.0"}}""");

        var (orchestrator, repoMock, scan) = BuildOrchestrator(_tempDir);

        await orchestrator.ExecuteAsync(scan.Id, null, new Progress<ScanProgress>(), CancellationToken.None);

        Assert.Equal(ScanStatus.Completed, scan.Status);
        Assert.NotNull(scan.CompletedAt);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Scan>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSourcePath_SavesFailedStatus()
    {
        var (orchestrator, _, scan) = BuildOrchestrator("/nonexistent/path/that/does/not/exist");

        await orchestrator.ExecuteAsync(scan.Id, null, new Progress<ScanProgress>(), CancellationToken.None);

        Assert.Equal(ScanStatus.Failed, scan.Status);
        Assert.NotNull(scan.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_SetsIsActiveInCode_BasedOnAnalyzer()
    {
        // package.json has "react", source file imports it
        File.WriteAllText(Path.Combine(_tempDir, "package.json"),
            """{"dependencies":{"react":"18.2.0","unused-lib":"1.0.0"}}""");
        File.WriteAllText(Path.Combine(_tempDir, "App.tsx"),
            "import React from 'react'\nexport default function App() { return null }");

        var (orchestrator, _, scan) = BuildOrchestrator(_tempDir);

        await orchestrator.ExecuteAsync(scan.Id, null, new Progress<ScanProgress>(), CancellationToken.None);

        Assert.Equal(ScanStatus.Completed, scan.Status);
        var react = scan.Technologies.FirstOrDefault(t => t.Name == "react");
        var unused = scan.Technologies.FirstOrDefault(t => t.Name == "unused-lib");
        Assert.NotNull(react);
        Assert.True(react.IsActiveInCode);
        Assert.NotNull(unused);
        Assert.False(unused.IsActiveInCode);
    }
}
