namespace MinIOCRUD.Dtos.Responses
{
    public class FolderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;

        // Minimal parent info to avoid cycles
        public ParentDto? Parent { get; set; }

        // Path and Breadcrumb
        public string Path { get; set; } = string.Empty;
        public List<BreadcrumbItemDto> Breadcrumb { get; set; } = new();

        // Files and Subfolders
        public List<FolderDto> SubFolders { get; set; } = new List<FolderDto>();
        public List<FileRecordDto> Files { get; set; } = new List<FileRecordDto>();

        
    }
}
