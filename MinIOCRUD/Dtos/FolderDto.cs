

namespace MinIOCRUD.Dtos
{
    public class FolderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;

        // Minimal parent info to avoid cycles
        public ParentDto? Parent { get; set; }

        public List<FolderDto> SubFolders { get; set; } = new List<FolderDto>();
        public List<FileRecordDto> Files { get; set; } = new List<FileRecordDto>();
    }
}
