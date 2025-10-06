using Minio;
using Minio.DataModel.Args;
using System.Runtime;

namespace MinIOCRUD.Services
{
    public class MinioService : IMinioService
    {

        private readonly IMinioClient _client;
        private readonly IMinioClient _publicClient;
        private readonly string _defaultBucket;
        private readonly IConfiguration _config;

        public MinioService(IConfiguration config)
        {
            var internalEndpoint = config["Minio:Endpoint"];
            var publicEndpoint = config["Minio:PublicEndpoint"];
            var accessKey = config["Minio:AccessKey"];
            var secretKey = config["Minio:SecretKey"];
            var secure = bool.Parse(config["Minio:Secure"] ?? "false");
            _defaultBucket = config["Minio:Bucket"] ?? "files";
            _config = config;


            _client = new MinioClient()
            .WithEndpoint(internalEndpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();

            _publicClient = new MinioClient()
            .WithEndpoint(publicEndpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();
        }

        public async Task DeleteObjectAsync(string bucket, string objectKey)
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey));
        }

        public async Task EnsureBucketExistsAsync(string bucket)
        {
            var found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!found)
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        }

        public async Task<Uri> GetPresignedGetObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry)
        {
            var url = await _publicClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry((int)expiry.TotalSeconds));

            var publicUrl = url.Replace(_config["Minio:Endpoint"]!, _config["Minio:PublicEndpoint"]);

            Console.WriteLine($"Generated public URL: {publicUrl}");
            Console.WriteLine($"Original URL: {url}");

            return new Uri(publicUrl);
        }

        public async Task PutObjectAsync(string bucket, string objectKey, Stream data, string contentType)
        {
            await EnsureBucketExistsAsync(bucket);
            await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithContentType(contentType)
            .WithObjectSize(data.Length));
        }

        public async Task<Uri> GetPresignedPutObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry)
        {
            await EnsureBucketExistsAsync(bucket);

            var url = await _publicClient.PresignedPutObjectAsync(new PresignedPutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry((int)expiry.TotalSeconds));

            return new Uri(url);
        }

        public async Task<(long Size, string ContentType)> StatObjectAsync(string bucket, string objectKey)
        {
            var stat = await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey));

            return (stat.Size, stat.ContentType);
        }

    }
}
