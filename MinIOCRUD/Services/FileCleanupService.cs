using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Models;
using System.Linq.Expressions;


namespace MinIOCRUD.Services
{

    /// <summary>
    /// Background worker that periodically removes expired or stale file records
    /// and corresponding MinIO objects.
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

            _interval = TimeSpan.FromMinutes(config.GetValue("FileCleanup:IntervalMinutes", 10));
            _pendingExpiry = TimeSpan.FromMinutes(config.GetValue("FileCleanup:PendingExpiryMinutes", 30));
            _failedExpiry = TimeSpan.FromDays(config.GetValue("FileCleanup:FailedExpiryDays", 1));
            _deletedExpiry = TimeSpan.FromDays(config.GetValue("FileCleanup:DeletedExpiryDays", 30));
            _dryRun = config.GetValue("FileCleanup:DryRun", true);
        }

        #region Background execution

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // cancellation-aware infinite loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope(); // async scope
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var minio = scope.ServiceProvider.GetRequiredService<IMinioService>();

                    var now = DateTimeOffset.UtcNow;

                    // Cleanup each category safely
                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.Status == "Pending" && f.Status != "Uploaded" && f.CreatedAt < now - _pendingExpiry,
                        reason: "Pending cleanup",
                        stoppingToken);

                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.Status == "Failed" && f.Status != "Uploaded" && f.UpdatedAt < now - _failedExpiry,
                        reason: "Failed cleanup",
                        stoppingToken);

                    await CleanupFilesAsync(db, minio,
                        predicate: f => f.IsDeleted && f.UpdatedAt < now - _deletedExpiry,
                        reason: "Deleted purge",
                        stoppingToken);


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

                await Task.Delay(_interval, stoppingToken);
            }
        }

        #endregion


        #region Cleanup helpers

        /// <summary>
        /// Finds and removes files matching a given condition and reason.
        /// </summary>
        private async Task CleanupFilesAsync(
            AppDbContext db,
            IMinioService minio,
            Expression<Func<FileRecord, bool>> predicate,
            string reason,
            CancellationToken ct)
        {
            // unified cleanup logic
            var files = await db.Files.Where(predicate).ToListAsync(ct);

            if (files.Count == 0)
                return;

            foreach (var file in files)
                await TryRemoveFile(file, db, minio, reason, ct);
        }

        /// <summary>
        /// Attempts to remove a file both from MinIO and the database.
        /// </summary>
        private async Task TryRemoveFile(FileRecord file, AppDbContext db, IMinioService minio, string reason, CancellationToken ct)
        {
            if (_dryRun)
            {
                _logger.LogInformation("[DryRun] {Reason}: Would remove file {FileName} ({Id})", reason, file.FileName, file.Id);
                return;
            }

            try
            {
                await minio.DeleteObjectAsync(file.Bucket, file.ObjectKey, ct);
                db.Files.Remove(file);
                _logger.LogInformation("{Reason}: Removed file {FileName} ({Id})", reason, file.FileName, file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Reason}: Could not remove file {Id}", reason, file.Id);
            }
        }

        #endregion

    }
}
