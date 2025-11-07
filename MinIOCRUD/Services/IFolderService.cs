using MinIOCRUD.Dtos.Responses;
using MinIOCRUD.Models;

namespace MinIOCRUD.Services
{
    /// <summary>
    /// Provides CRUD and retrieval operations for folders.
    /// </summary>
    public interface IFolderService
    {
        Task<Folder> CreateFolderAsync(Folder folder);

        Task DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default);

        Task<Folder?> GetFolderByIdAsync(Guid id);

        Task<FolderDto?> GetFolderDtoWithBreadcrumbsAsync(Guid id, CancellationToken cancellationToken);

        Task<(List<FolderDto>, List<FileRecordDto>)> GetRootContentsAsync();
    }
}