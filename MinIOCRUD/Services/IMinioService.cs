namespace MinIOCRUD.Services
{
    public interface IMinioService
    {
        Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken);
        Task PutObjectAsync(string bucket, string objectKey, Stream data, string contentType, CancellationToken cancellationToken);
        Task<Uri> GetPresignedPutObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);

        Task<Uri> GetPresignedGetObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);
        Task DeleteObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken);
        Task<(long Size, string ContentType)> StatObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    }
}
