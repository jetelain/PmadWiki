using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pmad.Wiki.Services;

/// <summary>
/// Background service that periodically cleans up old temporary media files.
/// </summary>
public sealed class TemporaryMediaCleanupService : BackgroundService
{
    private readonly ITemporaryMediaStorageService _storageService;
    private readonly ILogger<TemporaryMediaCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _fileMaxAge = TimeSpan.FromHours(24);

    public TemporaryMediaCleanupService(
        ITemporaryMediaStorageService storageService,
        ILogger<TemporaryMediaCleanupService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Temporary media cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                _logger.LogInformation("Running temporary media cleanup");
                await _storageService.CleanupOldTemporaryMediaAsync(_fileMaxAge, stoppingToken);
                _logger.LogInformation("Temporary media cleanup completed");
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temporary media cleanup");
            }
        }

        _logger.LogInformation("Temporary media cleanup service stopped");
    }
}
