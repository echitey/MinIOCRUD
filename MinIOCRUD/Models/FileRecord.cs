using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Models
{
    public class FileRecord
    {
        [Key]
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string SafeContentType { get; set; } = string.Empty;
        public string FriendlyContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Bucket { get; set; } = string.Empty;
        public string ObjectKey { get; set; } = string.Empty;
        public string UploaderId { get; set; } = string.Empty;

        public Guid? FolderId { get; set; }
        public Folder? Folder { get; set; }

        public int Version { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Metadata { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }
}
