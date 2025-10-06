using MinIOCRUD.Dtos;
using MinIOCRUD.Models;

namespace MinIOCRUD.Extensions
{
    public static class FileRecordExtensions
    {
        public static FileRecordDto ToDto(this FileRecord file)
        {
            return new FileRecordDto
            {
                Id = file.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SafeContentType = file.SafeContentType,
                FriendlyType = file.FriendlyContentType,
                Size = file.Size,
                Parent = file.Folder == null
                    ? null
                    : new ParentDto
                    {
                        Id = file.Folder.Id,
                        Name = file.Folder.Name
                    },
                CreatedAt = file.CreatedAt,
                Metadata = file.Metadata,
                Status = file.Status
            };
        }

        // ✅ List<FileRecord> → List<FileRecordDto>
        public static List<FileRecordDto> ToDtoList(this IEnumerable<FileRecord> files)
        {
            return files.Select(f => f.ToDto()).ToList();
        }
    }

}
