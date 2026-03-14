namespace TechScanner.Core.Interfaces;

public interface ISourceProvider
{
    Task<string> PrepareAsync(string input, CancellationToken ct = default);
    void Cleanup(string tempPath);
}
