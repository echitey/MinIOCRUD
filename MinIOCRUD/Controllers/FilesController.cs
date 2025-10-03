using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos;
using MinIOCRUD.Models;
using MinIOCRUD.Services;
using MinIOCRUD.Utils;
using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMinioService _minio;
        private readonly ILogger<FilesController> _logger;

        public FilesController(AppDbContext db, IMinioService minio, ILogger<FilesController> logger)
        {
            _db = db;
            _minio = minio;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FileRecord), StatusCodes.Status201Created)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0) return BadRequest("No file.");

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
                UploaderId = User?.Identity?.Name ?? "anonymous",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Uploaded"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
        }


        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var q = _db.Files
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
            var items = await q.ToListAsync();
            return Ok(items.ToDtoList());
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();
            return Ok(f.ToDto());
        }


        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();


            var url = await _minio.GetPresignedGetObjectUrlAsync(f.Bucket, f.ObjectKey, TimeSpan.FromMinutes(15));
            return Redirect(url.ToString());
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();


            // soft-delete
            f.IsDeleted = true;
            f.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();


            return NoContent();
        }

        [HttpDelete("{id}/hard")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var file = await _db.Files.FindAsync(id);
            if (file == null) return NotFound();

            try
            {
                await _minio.DeleteObjectAsync(file.Bucket, file.ObjectKey);
                _db.Files.Remove(file);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Hard deleted file {FileName} ({Id})", file.FileName, file.Id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hard delete failed for {FileName} ({Id})", file.FileName, file.Id);
                return StatusCode(500, "Failed to delete file from storage.");
            }
        }

        [HttpPost("hard-delete")]
        public async Task<IActionResult> HardDeleteBulk([FromBody] HardDeleteRequest request)
        {
            if (request == null || !request.Ids.Any())
                return BadRequest("No file IDs provided.");

            if (!request.Force)
                return BadRequest("Force flag must be true to perform bulk hard delete.");

            var files = await _db.Files.Where(f => request.Ids.Contains(f.Id)).ToListAsync();
            if (!files.Any()) return NotFound("No matching files found.");

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

            return NoContent();
        }


        [HttpPost("presign-upload")]
        [ProducesResponseType(typeof(PresignUploadResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignUploadRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName)) return BadRequest("FileName required.");

            var id = Guid.NewGuid();
            var bucket = "files";
            var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{id}_{request.FileName}";

            // Save placeholder in DB
            var record = new FileRecord
            {
                Id = id,
                FileName = request.FileName,
                ContentType = request.ContentType,
                Size = request.Size ?? 0,
                Bucket = bucket,
                ObjectKey = objectKey,
                UploaderId = User?.Identity?.Name ?? "anonymous",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            _db.Files.Add(record);
            await _db.SaveChangesAsync();

            // Generate presigned URL
            var url = await _minio.GetPresignedGetObjectUrlAsync(bucket, objectKey, TimeSpan.FromMinutes(15));

            var response = new PresignUploadResponse
            {
                FileId = id,
                UploadUrl = url.ToString(),
                ObjectKey = objectKey,
                Bucket = bucket
            };

            return Ok(response);
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmUpload(Guid id)
        {
            var record = await _db.Files.FindAsync(id);
            if (record == null) return NotFound();

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
                return BadRequest("File not found in MinIO or upload failed.");
            }

            return Ok(record.ToDto());
        }
    }
}
