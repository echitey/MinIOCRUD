namespace MinIOCRUD.Services
{
    public interface IMinioService
    {
        Task EnsureBucketExistsAsync(string bucket);
        Task PutObjectAsync(string bucket, string objectKey, Stream data, string contentType);
        Task<Uri> GetPresignedPutObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);

        Task<Uri> GetPresignedGetObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);
        Task DeleteObjectAsync(string bucket, string objectKey);
        Task<(long Size, string ContentType)> StatObjectAsync(string bucket, string objectKey);

    }
}
