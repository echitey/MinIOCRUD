using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Utils;

namespace MinIOCRUD.Services
{
    public class FileService : IFileService
    {
        private readonly AppDbContext _db;
        private readonly IMinioService _minio;
        private readonly ILogger<FileService> _logger;

        public FileService(AppDbContext db, IMinioService minio, ILogger<FileService> logger)
        {
            _db = db;
            _minio = minio;
            _logger = logger;
        }

        public async Task<FileRecordDto> UploadAsync(FileUploadRequest request, Guid? folderId)
        {
            if (folderId.HasValue)
            {
                var folder = await _db.Folders.FindAsync(folderId);
                if (folder == null) throw new KeyNotFoundException("Folder not found");
            }

            var file = request.File;
            if (file == null || file.Length == 0) throw new InvalidOperationException("No file provided.");

            var id = Guid.NewGuid();
            var bucket = "files";
            var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{id}_{file.FileName}";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            await _minio.PutObjectAsync(bucket, objectKey, ms, file.ContentType);

            var fileType = FileTypeHelper.ToSimpleFileType(file.ContentType, file.FileName);
            var friendlyType = Constants.ToFriendlyName(fileType);
            var safeContentType = FileTypeHelper.GetSafeContentType(file.ContentType, file.FileName);

            var record = new FileRecord
            {
                Id = id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SafeContentType = safeContentType,
                FriendlyContentType = friendlyType,
                Size = file.Length,
                Bucket = bucket,
                ObjectKey = objectKey,
                UploaderId = "anonymous",
                Version = 1,
                FolderId = folderId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Uploaded"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync();

            return record.ToDto();
        }

        public async Task<IEnumerable<FileRecordDto>> ListAsync(int page, int pageSize)
        {
            var items = await _db.Files
                .Where(f => !f.IsDeleted)
                .Include(f => f.Folder)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return items.ToDtoList();
        }

        public async Task<FileRecordDto?> GetByIdAsync(Guid id)
        {
            var f = await _db.Files
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == id);

            return f?.ToDto();
        }

        public async Task<string> GetDownloadUrlAsync(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) throw new KeyNotFoundException("File not found");

            var url = await _minio.GetPresignedGetObjectUrlAsync(f.Bucket, f.ObjectKey, TimeSpan.FromMinutes(15));
            return url.ToString();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) throw new KeyNotFoundException("File not found");

            f.IsDeleted = true;
            f.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task HardDeleteAsync(Guid id)
        {
            var file = await _db.Files.FindAsync(id);
            if (file == null) throw new KeyNotFoundException("File not found");

            try
            {
                await _minio.DeleteObjectAsync(file.Bucket, file.ObjectKey);
                _db.Files.Remove(file);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Hard deleted file {FileName} ({Id})", file.FileName, file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hard delete failed for {FileName} ({Id})", file.FileName, file.Id);
                throw;
            }
        }

        public async Task HardDeleteBulkAsync(HardDeleteRequest request)
        {
            if (request == null || !request.Ids.Any())
                throw new ArgumentException("No file IDs provided.");

            if (!request.Force)
                throw new InvalidOperationException("Force flag must be true to perform bulk hard delete.");

            var files = await _db.Files.Where(f => request.Ids.Contains(f.Id)).ToListAsync();
            foreach (var file in files)
            {
                try
                {
                    await _minio.DeleteObjectAsync(file.Bucket, file.ObjectKey);
                    _db.Files.Remove(file);
                    _logger.LogInformation("Hard deleted file {FileName} ({Id})", file.FileName, file.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FileName} ({Id})", file.FileName, file.Id);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<PresignUploadResponse> GetPresignedUploadUrlAsync(PresignUploadRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName))
                throw new ArgumentException("FileName required.");

            var id = Guid.NewGuid();
            var bucket = "files";
            var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{id}_{request.FileName}";

            var record = new FileRecord
            {
                Id = id,
                FileName = request.FileName,
                ContentType = request.ContentType,
                Size = request.Size ?? 0,
                Bucket = bucket,
                ObjectKey = objectKey,
                UploaderId = "anonymous",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync();

            var url = await _minio.GetPresignedGetObjectUrlAsync(bucket, objectKey, TimeSpan.FromMinutes(15));

            return new PresignUploadResponse
            {
                FileId = id,
                UploadUrl = url.ToString(),
                ObjectKey = objectKey,
                Bucket = bucket
            };
        }

        public async Task<FileRecordDto> ConfirmUploadAsync(Guid id)
        {
            var record = await _db.Files.FindAsync(id);
            if (record == null) throw new KeyNotFoundException("File not found");

            try
            {
                var (size, contentType) = await _minio.StatObjectAsync(record.Bucket, record.ObjectKey);
                record.Size = size;
                record.ContentType = contentType ?? record.ContentType;
                record.Status = "Uploaded";
                record.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch
            {
                record.Status = "Failed";
                record.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("File not found in MinIO or upload failed.");
            }

            return record.ToDto();
        }

        public async Task DeleteFilesInFolderAsync(Guid folderId)
        {
            var files = await _db.Files.Where(f => f.FolderId == folderId).ToListAsync();

            foreach (var file in files)
            {
                try
                {
                    await _minio.DeleteObjectAsync(file.Bucket, file.ObjectKey);
                    _db.Files.Remove(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FileName} in folder {FolderId}", file.FileName, folderId);
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
