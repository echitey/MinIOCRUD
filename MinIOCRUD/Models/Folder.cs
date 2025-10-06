using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinIOCRUD.Models
{
    public class Folder
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public Folder? Parent { get; set; }

        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public ICollection<FileRecord> Files { get; set; } = new List<FileRecord>();

        public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
