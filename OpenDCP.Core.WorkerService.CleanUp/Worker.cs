using OpenDCP.Core.DAL;
using Microsoft.EntityFrameworkCore;


namespace OpenDCP.Core.WorkerService.CleanUp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DCPCoreDbContext _dbContext;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, DCPCoreDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await base.StartAsync(cancellationToken);

            _logger.LogInformation("DCP Core CleanUp service started.");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DCP Core CleanUp background execution loop started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Initiating clean-up task.");
                    await PerformCleanUp();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred during clean-up cycle.");
                }

                try
                {
                    _logger.LogDebug("Waiting 1 hour before next clean-up cycle.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogTrace("Clean-up service execution loop was canceled due to shutdown.");
                    break;
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("DCP Core CleanUp service stopped.");
        }

        private async Task PerformCleanUp()
        {
            try
            {
                //Entities with TemporalTables - change tracking enabled
                //Nothing in core

                //Entities without change tracking
                int deletedGrants = await _dbContext.PersitedGrant
                                 .Where(x => x.CreationTime < DateTime.UtcNow.AddMinutes(-15))
                                 .ExecuteDeleteAsync();

                int deletedTokens = await _dbContext.RefreshToken
                    .Where(x => x.IsRevoked
                                || x.ExpirationDate > DateTime.UtcNow)
                    .ExecuteDeleteAsync();

                _logger.LogTrace("Clean-up completed successfully. Deleted {DeletedGrantsCount} persisted grants and {DeletedTokensCount} refresh tokens.", deletedGrants, deletedTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clean-up action not successful - see exception {Exception}", ex.Message);
            }

        }

    }
}
