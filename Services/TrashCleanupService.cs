using CloudStorage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudStorage.BackgroundServices
{
    public class TrashCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TrashCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

        public TrashCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TrashCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Trash Cleanup Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldTrashItemsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up old trash items");
                }

                // Wait for the next cleanup cycle
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Trash Cleanup Service is stopping");
        }

        private async Task CleanupOldTrashItemsAsync()
        {
            _logger.LogInformation("Starting cleanup of old deleted items");

            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            try
            {
                var count = await storageService.CleanupOldDeletedItemsAsync();
                
                if (count > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old deleted items (older than 15 days)", count);
                }
                else
                {
                    _logger.LogInformation("No old deleted items to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trash cleanup");
            }
        }
    }
}
