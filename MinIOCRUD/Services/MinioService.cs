using Minio;
using Minio.DataModel.Args;
using System.Runtime;

namespace MinIOCRUD.Services
{

    /// <summary>
    /// Service wrapper for MinIO operations like upload, download, delete, and presigned URLs.
    /// </summary>
    public class MinioService : IMinioService
    {

        private readonly IMinioClient _client;
        private readonly IMinioClient _publicClient;
        private readonly IConfiguration _config;

        private readonly string _defaultBucket;
        private readonly string _internalEndpoint;
        private readonly string _publicEndpoint;
        private readonly bool _useSsl;

        // Cache to avoid redundant bucket existence checks
        private readonly HashSet<string> _validatedBuckets = new();

        public MinioService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _internalEndpoint = config["Minio:Endpoint"] ?? "minio:9000";
            _publicEndpoint = config["Minio:PublicEndpoint"] ?? "localhost:9000";
            var accessKey = config["Minio:AccessKey"] ?? throw new InvalidOperationException("Minio:AccessKey missing");
            var secretKey = config["Minio:SecretKey"] ?? throw new InvalidOperationException("Minio:SecretKey missing");
            _useSsl = bool.TryParse(config["Minio:Secure"], out var secure) && secure;
            _defaultBucket = config["Minio:Bucket"] ?? "files";


            _client = new MinioClient()
                .WithEndpoint(_internalEndpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(_useSsl)
                .Build();

            _publicClient = new MinioClient()
                .WithEndpoint(_publicEndpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(_useSsl)
                .Build();
        }

        #region Bucket management

        /// <summary>
        /// Ensures a bucket exists, creating it if necessary. Uses in-memory cache to avoid repeated checks.
        /// </summary>
        public async Task EnsureBucketExistsAsync(string? bucket = null, CancellationToken cancellationToken = default)
        {
            bucket ??= _defaultBucket;

            if (_validatedBuckets.Contains(bucket))
                return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket),
                cancellationToken
            );

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucket),
                    cancellationToken
                );
            }

            _validatedBuckets.Add(bucket);
        }

        #endregion


        #region Object operations

        /// <summary>
        /// Uploads an object stream to MinIO.
        /// </summary>
        public async Task PutObjectAsync(
            string? bucket,
            string objectKey,
            Stream data,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            bucket ??= _defaultBucket;
            await EnsureBucketExistsAsync(bucket, cancellationToken);

            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithStreamData(data)
                .WithContentType(contentType)
                .WithObjectSize(data.Length),
                cancellationToken);
        }

        /// <summary>
        /// Deletes an object from the specified bucket.
        /// </summary>
        public async Task DeleteObjectAsync(
            string? bucket,
            string objectKey,
            CancellationToken cancellationToken = default)
        {
            bucket ??= _defaultBucket;

            await _client.RemoveObjectAsync(
                new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey),
                cancellationToken
            );
        }

        /// <summary>
        /// Gets metadata (size and content type) of an object.
        /// </summary>
        public async Task<(long Size, string ContentType)> StatObjectAsync(
            string? bucket,
            string objectKey,
            CancellationToken cancellationToken = default)
        {
            bucket ??= _defaultBucket;

            var stat = await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(bucket).WithObject(objectKey),
                cancellationToken
            );

            return (stat.Size, stat.ContentType);
        }

        #endregion

        #region Presigned URLs

        /// <summary>
        /// Generates a presigned GET URL for downloading an object.
        /// </summary>
        public async Task<Uri> GetPresignedGetObjectUrlAsync(
            string? bucket,
            string objectKey,
            TimeSpan expiry)
        {
            bucket ??= _defaultBucket;

            var url = await _publicClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithExpiry((int)expiry.TotalSeconds)
            );

            return new Uri(ReplaceInternalWithPublic(url));
        }

        /// <summary>
        /// Generates a presigned PUT URL for uploading an object directly to MinIO.
        /// </summary>
        public async Task<Uri> GetPresignedPutObjectUrlAsync(
            string? bucket,
            string objectKey,
            TimeSpan expiry)
        {
            bucket ??= _defaultBucket;
            await EnsureBucketExistsAsync(bucket);

            var url = await _publicClient.PresignedPutObjectAsync(
                new PresignedPutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithExpiry((int)expiry.TotalSeconds)
            );

            return new Uri(ReplaceInternalWithPublic(url));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Replaces internal MinIO endpoint with the public endpoint for external access.
        /// </summary>
        private string ReplaceInternalWithPublic(string url) =>
            url.Replace(_internalEndpoint, _publicEndpoint, StringComparison.OrdinalIgnoreCase);

        #endregion

    }
}
