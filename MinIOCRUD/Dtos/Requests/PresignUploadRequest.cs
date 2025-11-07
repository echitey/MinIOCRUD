namespace MinIOCRUD.Dtos.Requests
{
    public class PresignUploadRequest
    {
        /// <summary>
        /// Original filename (used for metadata)
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the file (e.g. application/pdf, image/png)
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Size in bytes (optional, but helps validation)
        /// </summary>
        public long? Size { get; set; }
    }
}
