using MinIOCRUD.Dtos;
using MinIOCRUD.Dtos.Responses;
using MinIOCRUD.Models;

namespace MinIOCRUD.Extensions
{
    public static class FolderExtensions
    {

        /// <summary>
        /// Maps a Folder entity to FolderDto, computing Path and Breadcrumb dynamically.
        /// </summary>
        public static FolderDto ToDtoWithBreadcrumb(this Folder folder, List<BreadcrumbItemDto> breadcrumb)
        {

            // Compute path as a simple joined string
            var path = string.Join("/", breadcrumb.Select(b => b.Name));

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
                Path = path,
                Breadcrumb = breadcrumb,
                SubFolders = folder.SubFolders.Select(f => f.ToDtoWithBreadcrumb(breadcrumb)).ToList(),
                Files = folder.Files.Select(fr => fr.ToDto()).ToList(),
            };
        }

    }
}
