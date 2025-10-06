using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Dtos
{
    public class FileRecordDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string SafeContentType { get; set; } = string.Empty;
        public string FriendlyType { get; set; } = string.Empty;
        public long Size { get; set; }
        public ParentDto? Parent { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Metadata { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }
}
