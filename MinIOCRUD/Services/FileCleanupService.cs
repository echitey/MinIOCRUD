using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Models;
using System.Linq.Expressions;

namespace MinIOCRUD.Services
{
    /// <summary>
    /// Background worker that periodically scans the database for expired, failed, or deleted files
    /// and removes them from both the database and MinIO storage.
    /// </summary>
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileCleanupService> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _pendingExpiry;
        private readonly TimeSpan _failedExpiry;
        private readonly TimeSpan _deletedExpiry;
        private readonly bool _dryRun;

        public FileCleanupService(IServiceScopeFactory scopeFactory, ILogger<FileCleanupService> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // Interval between cleanup runs (default 10 minutes)
            _interval = TimeSpan.FromMinutes(config.GetValue("FileCleanup:IntervalMinutes", 10));

            // Files that are "Pending" for too long (default 60 minutes) are cleaned up.
            _pendingExpiry = TimeSpan.FromMinutes(config.GetValue("FileCleanup:PendingExpiryMinutes", 60));

            // Files marked as "Failed" for too long (default 1 day) are removed.
            _failedExpiry = TimeSpan.FromDays(config.GetValue("FileCleanup:FailedExpiryDays", 1));

            // Files that were soft-deleted and exceed this retention (default 30 days) are purged permanently.
            _deletedExpiry = TimeSpan.FromDays(config.GetValue("FileCleanup:DeletedExpiryDays", 30));

            // Dry-run mode: when true, no deletions are actually performed — only logs are emitted.
            // Useful for testing or monitoring cleanup behavior safely in staging environments.
            _dryRun = config.GetValue("FileCleanup:DryRun", true);
        }

        #region Background execution

        /// <summary>
        /// Periodically executes cleanup tasks in a continuous loop.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Runs indefinitely until the service is stopped.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var minio = scope.ServiceProvider.GetRequiredService<IMinioService>();

                    var now = DateTimeOffset.UtcNow;

                    // Category 1: Pending cleanup
                    // Removes files that are still "Pending" but have not been uploaded after a certain expiry window.
                    // These typically represent unfinished uploads or client-side interruptions.
                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.Status == "Pending" && f.Status != "Uploaded" && f.CreatedAt < now - _pendingExpiry,
                        reason: "Pending cleanup",
                        stoppingToken);

                    // Category 2: Failed cleanup
                    // Removes files with upload status "Failed" that haven’t been retried or updated recently.
                    // Prevents stale entries from accumulating in the database.
                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.Status == "Failed" && f.Status != "Uploaded" && f.UpdatedAt < now - _failedExpiry,
                        reason: "Failed cleanup",
                        stoppingToken);

                    // Category 3: Deleted purge
                    // Permanently purges files that were soft-deleted and have exceeded the retention period.
                    // This is the final deletion step for user-removed files.
                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.IsDeleted && f.UpdatedAt < now - _deletedExpiry,
                        reason: "Deleted purge",
                        stoppingToken);

                    // Save changes to the database only if not in dry-run mode.
                    if (!_dryRun)
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("File cleanup service stopping...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "File cleanup cycle failed.");
                }

                // Wait until next scheduled cleanup
                await Task.Delay(_interval, stoppingToken);
            }
        }

        #endregion


        #region Cleanup helpers

        /// <summary>
        /// Finds and processes all files that match a specific cleanup condition.
        /// </summary>
        /// <param name="db">Database context instance.</param>
        /// <param name="minio">MinIO service for object deletion.</param>
        /// <param name="predicate">LINQ expression defining which files to clean.</param>
        /// <param name="reason">Description of the cleanup reason for logging.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task CleanupFilesAsync(
            AppDbContext db,
            IMinioService minio,
            Expression<Func<FileRecord, bool>> predicate,
            string reason,
            CancellationToken ct)
        {
            var files = await db.Files.Where(predicate).ToListAsync(ct);

            if (files.Count == 0)
                return;

            foreach (var file in files)
                await TryRemoveFile(file, db, minio, reason, ct);
        }

        /// <summary>
        /// Attempts to remove a file record both from MinIO and the database.
        /// </summary>
        /// <param name="file">The file record to remove.</param>
        /// <param name="db">Database context.</param>
        /// <param name="minio">MinIO service.</param>
        /// <param name="reason">Cleanup reason for log output.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task TryRemoveFile(FileRecord file, AppDbContext db, IMinioService minio, string reason, CancellationToken ct)
        {
            if (_dryRun)
            {
                // Log what WOULD have been deleted without performing any destructive action.
                _logger.LogInformation("[DryRun] {Reason}: Would remove file {FileName} ({Id})", reason, file.FileName, file.Id);
                return;
            }

            try
            {
                // Attempt to delete the object from MinIO
                await minio.DeleteObjectAsync(file.Bucket, file.ObjectKey, ct);

                // Remove database record
                db.Files.Remove(file);

                _logger.LogInformation("{Reason}: Removed file {FileName} ({Id})", reason, file.FileName, file.Id);
            }
            catch (Exception ex)
            {
                // Continue processing other files even if one deletion fails
                _logger.LogWarning(ex, "{Reason}: Could not remove file {Id}", reason, file.Id);
            }
        }

        #endregion
    }
}
