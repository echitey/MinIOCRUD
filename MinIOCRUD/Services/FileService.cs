using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Utils;

namespace MinIOCRUD.Services
{

    /// <summary>
    /// Handles file upload, download, deletion, and presigned URL operations backed by MinIO.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly AppDbContext _db;
        private readonly IMinioService _minio;
        private readonly ILogger<FileService> _logger;
        private readonly string _defaultBucket = "files";

        public FileService(AppDbContext db, IMinioService minio, ILogger<FileService> logger)
        {
            _db = db;
            _minio = minio;
            _logger = logger;
        }

        #region Upload

        public async Task<FileRecordDto> UploadAsync(FileUploadRequest request, Guid? folderId, CancellationToken cancellationToken = default)
        {
            if (request.File == null || request.File.Length == 0)
                throw new InvalidOperationException("No file provided.");

            if (folderId.HasValue && !await FolderExistsAsync(folderId.Value, cancellationToken))
                throw new KeyNotFoundException("Folder not found.");

            var id = Guid.NewGuid();
            var sanitizedName = request.File.FileName.SanitizeFileName();
            var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{id}_{sanitizedName}";

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            await _minio.PutObjectAsync(_defaultBucket, objectKey, ms, request.File.ContentType, cancellationToken);

            var fileType = FileTypeHelper.ToSimpleFileType(request.File.ContentType, sanitizedName);
            var record = new FileRecord
            {
                Id = id,
                FileName = sanitizedName,
                ContentType = request.File.ContentType,
                SafeContentType = FileTypeHelper.GetSafeContentType(request.File.ContentType, sanitizedName),
                FriendlyContentType = Constants.ToFriendlyName(fileType),
                Size = request.File.Length,
                Bucket = _defaultBucket,
                ObjectKey = objectKey,
                UploaderId = "anonymous",
                Version = 1,
                FolderId = folderId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Uploaded"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync(cancellationToken);

            return record.ToDto();
        }

        #endregion

        #region Queries

        public async Task<IEnumerable<FileRecordDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var items = await _db.Files
                .Where(f => !f.IsDeleted)
                .Include(f => f.Folder)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return items.ToDtoList();
        }

        public async Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _db.Files
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            return file?.ToDto();
        }

        public async Task<string> GetDownloadUrlAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _db.Files.FindAsync([id], cancellationToken);
            if (file == null) throw new KeyNotFoundException("File not found");

            var url = await _minio.GetPresignedGetObjectUrlAsync(file.Bucket, file.ObjectKey, TimeSpan.FromMinutes(15));
            return url.ToString();
        }

        #endregion


        #region Delete operations

        public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _db.Files.FindAsync([id], cancellationToken);
            if (file == null) throw new KeyNotFoundException("File not found");

            file.IsDeleted = true;
            file.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _db.Files.FindAsync([id], cancellationToken);
            if (file == null) throw new KeyNotFoundException("File not found");

            await SafeDeleteFromMinioAsync(file, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task HardDeleteBulkAsync(HardDeleteRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || !request.Ids.Any())
                throw new ArgumentException("No file IDs provided.");

            if (!request.Force)
                throw new InvalidOperationException("Force flag must be true to perform bulk hard delete.");

            var files = await _db.Files.Where(f => request.Ids.Contains(f.Id)).ToListAsync(cancellationToken);
            foreach (var file in files)
                await SafeDeleteFromMinioAsync(file, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteFilesInFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
        {
            var files = await _db.Files.Where(f => f.FolderId == folderId).ToListAsync(cancellationToken);
            foreach (var file in files)
                await SafeDeleteFromMinioAsync(file, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SafeDeleteFromMinioAsync(FileRecord file, CancellationToken cancellationToken)
        {
            try
            {

                await _minio.DeleteObjectAsync(file.Bucket, file.ObjectKey, cancellationToken);
                _db.Files.Remove(file);
                _logger.LogInformation("Deleted file {FileName} ({Id})", file.FileName, file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {FileName} ({Id})", file.FileName, file.Id);
            }
        }

        #endregion


        #region Presigned uploads

        public async Task<PresignUploadResponse> GetPresignedUploadUrlAsync(PresignUploadRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.FileName))
                throw new ArgumentException("FileName required.");

            var id = Guid.NewGuid();
            var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{id}_{request.FileName.SanitizeFileName()}";

            var record = new FileRecord
            {
                Id = id,
                FileName = request.FileName.SanitizeFileName(),
                ContentType = request.ContentType,
                Size = request.Size ?? 0,
                Bucket = _defaultBucket,
                ObjectKey = objectKey,
                UploaderId = "anonymous",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync(cancellationToken);

            var url = await _minio.GetPresignedPutObjectUrlAsync(_defaultBucket, objectKey, TimeSpan.FromMinutes(15));
            return new PresignUploadResponse
            {
                FileId = id,
                UploadUrl = url.ToString(),
                ObjectKey = objectKey,
                Bucket = _defaultBucket
            };
        }

        public async Task<FileRecordDto> ConfirmUploadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var record = await _db.Files.FindAsync([id], cancellationToken);
            if (record == null) throw new KeyNotFoundException("File not found");

            try
            {
                var (size, contentType) = await _minio.StatObjectAsync(record.Bucket, record.ObjectKey, cancellationToken);
                record.Size = size;
                record.ContentType = contentType ?? record.ContentType;
                record.Status = "Uploaded";
            }
            catch
            {
                record.Status = "Failed";
                throw new InvalidOperationException("File not found in MinIO or upload failed.");
            }
            finally
            {
                record.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return record.ToDto();
        }

        #endregion

        #region Helpers

        private async Task<bool> FolderExistsAsync(Guid folderId, CancellationToken cancellationToken)
        {
            return await _db.Folders.AnyAsync(f => f.Id == folderId, cancellationToken);
        }

        #endregion
    }
}
