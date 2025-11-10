using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BettsTax.Core.Options;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BettsTax.Core.Services
{
    public class DocumentRetentionBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DocumentRetentionBackgroundService> _logger;
        private readonly IOptions<DocumentRetentionOptions> _options;

        public DocumentRetentionBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<DocumentRetentionOptions> options,
            ILogger<DocumentRetentionBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DocumentRetentionBackgroundService started");
            // Initial small delay to allow app to fully start
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (TaskCanceledException) { }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running document retention cycle");
                }

                var intervalMinutes = Math.Max(1, _options.Value.IntervalMinutes);
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // shutting down
                }
            }

            _logger.LogInformation("DocumentRetentionBackgroundService stopping");
        }

        private async Task ProcessOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            var now = DateTime.UtcNow;
            var retentionDays = Math.Max(0, _options.Value.RetentionDays);
            var keepMin = Math.Max(1, _options.Value.KeepMinVersions);
            var graceDays = Math.Max(0, _options.Value.PhysicalDeleteGraceDays);
            var batchSize = Math.Max(50, _options.Value.BatchSize);

            var retentionCutoff = now.AddDays(-retentionDays);
            var graceCutoff = now.AddDays(-graceDays);

            // Phase 1: Soft-delete old versions beyond keep-min policy
            var softCandidates = await db.DocumentVersions
                .Where(v => !v.IsDeleted && v.UploadedAt <= retentionCutoff)
                .Join(db.Documents,
                      v => v.DocumentId,
                      d => d.DocumentId,
                      (v, d) => new { v, d })
                .Where(x => x.v.VersionNumber <= x.d.CurrentVersionNumber - keepMin)
                .OrderBy(x => x.v.UploadedAt)
                .Take(batchSize)
                .ToListAsync(ct);

            if (softCandidates.Count > 0)
            {
                foreach (var x in softCandidates)
                {
                    x.v.IsDeleted = true;
                    x.v.DeletedAt = now;
                    x.v.DeletedById = null; // system
                }

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Soft-deleted {Count} document version(s) as part of retention policy", softCandidates.Count);
            }

            // Phase 2: Physically delete files for versions past grace period
            var purgeCandidates = await db.DocumentVersions
                .Where(v => v.IsDeleted && v.DeletedAt != null && v.DeletedAt <= graceCutoff)
                .OrderBy(v => v.DeletedAt)
                .Take(batchSize)
                .ToListAsync(ct);

            int purgedFiles = 0;
            foreach (var v in purgeCandidates)
            {
                try
                {
                    var exists = await storage.FileExistsAsync(v.StoragePath);
                    if (exists)
                    {
                        await storage.DeleteFileAsync(v.StoragePath);
                        purgedFiles++;
                    }
                    else
                    {
                        _logger.LogDebug("File not found for DocumentVersion {DocumentVersionId} at {Path}; skipping", v.DocumentVersionId, v.StoragePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to physically delete file for DocumentVersion {DocumentVersionId}", v.DocumentVersionId);
                }
            }

            if (purgedFiles > 0)
            {
                _logger.LogInformation("Physically deleted {Count} document version file(s) as part of retention policy", purgedFiles);
            }
        }
    }
}
