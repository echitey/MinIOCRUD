using MinIOCRUD.Dtos;
using MinIOCRUD.Models;

namespace MinIOCRUD.Extensions
{
    public static class FolderExtensions
    {
        public static FolderDto ToDto(this Folder folder)
        {
            if (folder == null) return null!;

            return new FolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                Parent = folder.Parent == null
                    ? null
                    : new ParentDto
                    {
                        Id = folder.Parent.Id,
                        Name = folder.Parent.Name
                    },
                SubFolders = folder.SubFolders.Select(f => f.ToDto()).ToList(),
                Files = folder.Files.Select(fr => fr.ToDto()).ToList()
            };
        }
    }
}
