using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos.Responses;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using System.Xml.Linq;

namespace MinIOCRUD.Services
{

    /// <summary>
    /// Handles folder operations: create, delete (recursive), retrieve folders and root contents.
    /// </summary>
    public class FolderService : IFolderService
    {
        private readonly AppDbContext _db;
        private readonly IMinioService _minio;
        private readonly ILogger<FolderService> _logger;
        private readonly string _bucketName;

        public FolderService(AppDbContext db, IMinioService minio, IConfiguration config, ILogger<FolderService> logger)
        {
            _db = db;
            _minio = minio;
            _logger = logger;
            _bucketName = config.GetValue<string>("Minio:Bucket") ?? "files";
        }


        #region Folder CRUD

        /// <summary>
        /// Creates a new folder in the database.
        /// </summary>
        public async Task<Folder> CreateFolderAsync(Folder folder)
        {
            await _db.Folders.AddAsync(folder);
            await _db.SaveChangesAsync();
            return folder;
        }

        /// <summary>
        /// Retrieves a folder by ID including its subfolders and files.
        /// </summary>
        public async Task<Folder?> GetFolderByIdAsync(Guid id)
        {
            return await _db.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .Include(f => f.Parent)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>
        /// Retrieves a folder DTO with its breadcrumb path.
        /// </summary>
        public async Task<FolderDto?> GetFolderDtoWithBreadcrumbsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var folder = await _db.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .Include(f => f.Parent)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            if (folder == null)
                throw new KeyNotFoundException("Folder not found.");

            var breadcrumb = new List<BreadcrumbItemDto>();
            var current = folder;

            while (current != null)
            {
                breadcrumb.Insert(0, new BreadcrumbItemDto
                {
                    Id = current.Id,
                    Name = current.Name
                });

                if (current.ParentId == null) break;

                current = await _db.Folders.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == current.ParentId, cancellationToken);
            }

            return folder.ToDtoWithBreadcrumb(breadcrumb);
        }

        #endregion


        #region Root contents

        /// <summary>
        /// Retrieves all root folders and root files (no parent).
        /// </summary>
        public async Task<(List<FolderDto>, List<FileRecordDto>)> GetRootContentsAsync()
        {
            var roots = await _db.Folders
                .Where(f => f.ParentId == null)
                .AsNoTracking()
                .ToListAsync();

            var rootFiles = await _db.Files
                .Where(f => f.FolderId == null)
                .AsNoTracking()
                .ToListAsync();

            var folders = roots.Select(f => f.ToDtoWithBreadcrumb([])).ToList();
            var files = rootFiles.Select(fr => fr.ToDto()).ToList();

            return (folders, files);
        }

        #endregion


        #region Delete folder

        /// <summary>
        /// Deletes a folder and all its contents recursively.
        /// </summary>
        public async Task DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var folder = await _db.Folders
                    .Include(f => f.SubFolders)
                    .Include(f => f.Files)
                    .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);

                if (folder == null)
                    throw new KeyNotFoundException("Folder not found.");

                await DeleteFolderRecursive(folder, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete folder {FolderId}", folderId);
                throw;
            }
        }

        /// <summary>
        /// Helper method for recursive folder deletion.
        /// </summary>
        private async Task DeleteFolderRecursive(Folder folder, CancellationToken cancellationToken)
        {
            foreach (var sub in folder.SubFolders.ToList())
                await DeleteFolderRecursive(sub, cancellationToken);

            foreach (var file in folder.Files.ToList())
            {
                try
                {
                    await _minio.DeleteObjectAsync(_bucketName, file.ObjectKey, cancellationToken);
                    _db.Files.Remove(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FileName} in folder {FolderId}", file.FileName, folder.Id);
                }
            }

            _db.Folders.Remove(folder);
        }

        #endregion


    }
}
