using MinIOCRUD.Dtos;
using MinIOCRUD.Models;

namespace MinIOCRUD.Services
{
    public interface IFolderService
    {
        Task<Folder> CreateFolderAsync(Folder folder);
        Task DeleteFolderAsync(Guid folderId);
        Task<Folder?> GetFolderAsync(Guid id);
        Task<FolderDto?> GetFolderDtoWithBreadcrumbsAsync(Guid id);
        Task<(List<FolderDto>, List<FileRecordDto>)> GetRootFoldersAsync();
    }
}