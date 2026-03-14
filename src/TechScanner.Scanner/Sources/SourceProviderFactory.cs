using TechScanner.Core.Enums;
using TechScanner.Core.Interfaces;

namespace TechScanner.Scanner.Sources;

public class SourceProviderFactory
{
    private readonly LocalFolderProvider _localFolder;
    private readonly ZipArchiveProvider _zipArchive;
    private readonly GitRepoProvider _gitRepo;

    public SourceProviderFactory(
        LocalFolderProvider localFolder,
        ZipArchiveProvider zipArchive,
        GitRepoProvider gitRepo)
    {
        _localFolder = localFolder;
        _zipArchive = zipArchive;
        _gitRepo = gitRepo;
    }

    public ISourceProvider GetProvider(SourceType type) => type switch
    {
        SourceType.LocalFolder => _localFolder,
        SourceType.ZipArchive => _zipArchive,
        SourceType.GitRepository => _gitRepo,
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown source type: {type}")
    };
}
