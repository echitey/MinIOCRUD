using MinIOCRUD.Dtos;

namespace MinIOCRUD.Services
{
    public interface IFileService
    {
        Task<FileRecordDto> UploadAsync(FileUploadRequest request, Guid? folderId);
        Task<IEnumerable<FileRecordDto>> ListAsync(int page, int pageSize);
        Task<FileRecordDto?> GetByIdAsync(Guid id);
        Task<string> GetDownloadUrlAsync(Guid id);
        Task SoftDeleteAsync(Guid id);
        Task HardDeleteAsync(Guid id);
        Task HardDeleteBulkAsync(HardDeleteRequest request);
        Task<PresignUploadResponse> GetPresignedUploadUrlAsync(PresignUploadRequest request);
        Task<FileRecordDto> ConfirmUploadAsync(Guid id);
        Task DeleteFilesInFolderAsync(Guid folderId);
    }
}
