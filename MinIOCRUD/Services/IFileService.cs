using MinIOCRUD.Dtos;

namespace MinIOCRUD.Services
{
    /// <summary>
    /// Provides CRUD and presigned URL management for file records stored in MinIO.
    /// </summary>
    public interface IFileService
    {
        Task<FileRecordDto> UploadAsync(FileUploadRequest request, Guid? folderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<FileRecordDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<string> GetDownloadUrlAsync(Guid id, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task HardDeleteBulkAsync(HardDeleteRequest request, CancellationToken cancellationToken = default);
        Task<PresignUploadResponse> GetPresignedUploadUrlAsync(PresignUploadRequest request, CancellationToken cancellationToken = default);
        Task<FileRecordDto> ConfirmUploadAsync(Guid id, CancellationToken cancellationToken = default);
        Task DeleteFilesInFolderAsync(Guid folderId, CancellationToken cancellationToken = default);
    }
}
