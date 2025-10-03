using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Models;


namespace MinIOCRUD.Services
{
    
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileCleanupService> _logger;
        private readonly IConfiguration _config;

        public FileCleanupService(IServiceScopeFactory scopeFactory, ILogger<FileCleanupService> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(_config.GetValue<int>("FileCleanup:IntervalMinutes", 10));
            var pendingExpiry = TimeSpan.FromMinutes(_config.GetValue<int>("FileCleanup:PendingExpiryMinutes", 30));
            var failedExpiry = TimeSpan.FromDays(_config.GetValue<int>("FileCleanup:FailedExpiryDays", 1));
            var deletedExpiry = TimeSpan.FromDays(_config.GetValue<int>("FileCleanup:DeletedExpiryDays", 30));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var _minio = scope.ServiceProvider.GetRequiredService<IMinioService>();

                    var now = DateTimeOffset.UtcNow;

                    // 1. Pending -> cleanup
                    var stalePending = await db.Files
                        .Where(f => f.Status == "Pending" && f.CreatedAt < now - pendingExpiry)
                        .ToListAsync(stoppingToken);

                    foreach (var file in stalePending)
                        await TryRemoveFile(file, db, _minio, "Pending cleanup");

                    // 2. Failed -> cleanup
                    var staleFailed = await db.Files
                        .Where(f => f.Status == "Failed" && f.UpdatedAt < now - failedExpiry)
                        .ToListAsync(stoppingToken);

                    foreach (var file in staleFailed)
                        await TryRemoveFile(file, db, _minio, "Failed cleanup");

                    // 3. Soft-deleted -> purge
                    var staleDeleted = await db.Files
                        .Where(f => f.IsDeleted && f.UpdatedAt < now - deletedExpiry)
                        .ToListAsync(stoppingToken);

                    foreach (var file in staleDeleted)
                        await TryRemoveFile(file, db, _minio, "Deleted purge");

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "File cleanup service failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task TryRemoveFile(FileRecord file, AppDbContext db, IMinioService minio, string reason)
        {
            try
            {
                await minio.DeleteObjectAsync(file.Bucket, file.ObjectKey);
                db.Files.Remove(file);
                _logger.LogInformation("{Reason}: Removed file {FileName} ({Id})", reason, file.FileName, file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Reason}: Could not remove file {Id}", reason, file.Id);
            }
        }
    }
}
