using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using System.Xml.Linq;

namespace MinIOCRUD.Services
{
    public class FolderService : IFolderService
    {
        private readonly AppDbContext _db;
        private readonly IMinioService _minio;
        private readonly IConfiguration _config;

        public FolderService(AppDbContext db, IMinioService minio, IConfiguration config)
        {
            _db = db;
            _minio = minio;
            _config = config;
        }

        public async Task<Folder> CreateFolderAsync(Folder folder)
        {
            await _db.Folders.AddAsync(folder);
            await _db.SaveChangesAsync();

            return folder;
        }

        public async Task<Folder?> GetFolderAsync(Guid id)
        {
            return await _db.Folders
               .Include(f => f.SubFolders)
               .Include(f => f.Files)
               .Include(f => f.Parent)
               .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<FolderDto?> GetFolderDtoWithBreadcrumbsAsync(Guid id)
        {
            var folder = await _db.Folders
               .Include(f => f.SubFolders)
               .Include(f => f.Files)
               .Include(f => f.Parent)
               .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                throw new Exception("Folder not found");

            var breadcrumb = new List<BreadcrumbItemDto>();
            var current = folder;

            while (current != null)
            {
                breadcrumb.Insert(0, new BreadcrumbItemDto
                {
                    Id = current.Id,
                    Name = current.Name
                });

                if (current.ParentId == null)
                    break;

                current = await _db.Folders.FirstOrDefaultAsync(f => f.Id == current.ParentId);
            }

            return folder.ToDtoWithBreadcrumb(breadcrumb);
        }

        // Get root folders with subfolders and files
        public async Task<(List<FolderDto>, List<FileRecordDto>)> GetRootFoldersAsync()
        {
            var roots = await _db.Folders
                .Where(f => f.ParentId == null)
                .ToListAsync();

            var rootfiles = await _db.Files
                .Where(f => f.FolderId == null)
                .ToListAsync();

            var result = roots.Select(f => f.ToDtoWithBreadcrumb(new List<BreadcrumbItemDto>())).ToList();
            var files = rootfiles.Select(fr => fr.ToDto()).ToList();

            return (result, files);
        }

        // Delete folder recursively including files
        public async Task DeleteFolderAsync(Guid folderId)
        {
            //var _bucketName = _config.GetValue<string>("Minio:Bucket") ?? "files";
            var _bucketName = _config?.GetSection("Minio:Bucket")?.Value ?? "files";

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var folder = await _db.Folders
                    .Include(f => f.SubFolders)
                    .Include(f => f.Files)
                    .FirstOrDefaultAsync(f => f.Id == folderId);

                if (folder == null)
                    throw new Exception("Folder not found.");

                await DeleteFolderRecursive(folder, _bucketName);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Recursive deletion helper
        private async Task DeleteFolderRecursive(Folder folder, string bucketName)
        {
            // Delete subfolders first
            foreach (var sub in folder.SubFolders.ToList())
            {
                await DeleteFolderRecursive(sub, bucketName);
            }

            // Delete files in MinIO
            foreach (var file in folder.Files.ToList())
            {
                try
                {
                    await _minio.DeleteObjectAsync(bucketName, file.ObjectKey);
                }
                catch (Exception ex)
                {
                    // Log and continue, maybe file missing
                    Console.WriteLine($"Failed to delete file {file.FileName}: {ex.Message}");
                }

                _db.Files.Remove(file);
            }

            _db.Folders.Remove(folder);
            await _db.SaveChangesAsync();
        }

    }
}
