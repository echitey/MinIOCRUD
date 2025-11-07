namespace MinIOCRUD.Dtos.Responses
{
    public class PresignUploadResponse
    {
        public Guid FileId { get; set; }
        public string UploadUrl { get; set; } = string.Empty;
        public string ObjectKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
    }

}
