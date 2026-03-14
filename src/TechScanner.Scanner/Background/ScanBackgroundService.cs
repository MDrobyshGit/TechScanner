using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechScanner.Scanner.Orchestrator;

namespace TechScanner.Scanner.Background;

public class ScanBackgroundService : BackgroundService
{
    private readonly Channel<ScanJob> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScanBackgroundService> _logger;
    public readonly ConcurrentDictionary<Guid, ScanProgress> ProgressMap = new();

    public ScanBackgroundService(
        Channel<ScanJob> channel,
        IServiceProvider serviceProvider,
        ILogger<ScanBackgroundService> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            ProgressMap[job.ScanId] = new ScanProgress(0, "Queued...");
            _logger.LogInformation("Processing scan job {ScanId}", job.ScanId);

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ScanOrchestrator>();

            var progress = new Progress<ScanProgress>(p =>
            {
                ProgressMap[job.ScanId] = p;
            });

            try
            {
                await orchestrator.ExecuteAsync(job.ScanId, job.GitToken, progress, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in scan {ScanId}", job.ScanId);
                ProgressMap[job.ScanId] = new ScanProgress(100, $"Fatal error: {ex.Message}");
            }
        }
    }
}
