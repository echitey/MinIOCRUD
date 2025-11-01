using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Services;

namespace MinIOCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoldersController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly IFolderService _folderService;

        public FoldersController(AppDbContext db, IFolderService folderService)
        {
            _db = db;
            _folderService = folderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromQuery] string name, [FromQuery] Guid? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ErrorResponse("Folder name is required", 400);

            var folder = new Folder { Name = name, ParentId = parentId };
            await _folderService.CreateFolderAsync(folder);

            return folder.Id == Guid.Empty
                ? ErrorResponse("Folder creation failed", 400)
                : CreatedResponse(folder.ToDtoWithBreadcrumb(new List<Dtos.BreadcrumbItemDto>()));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFolder(Guid id, CancellationToken cancellationToken = default)
        {
            var folder = await _folderService.GetFolderDtoWithBreadcrumbsAsync(id, cancellationToken);

            return folder == null
                ? ErrorResponse("Folder not found", 404)
                : OkResponse(folder);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFolder(Guid id)
        {
            try
            {
                await _folderService.DeleteFolderAsync(id);
                return NoContent();
            }
            catch (Exception)
            {
                // Logging handled at middleware level
                return ErrorResponse("Error occurred while deleting the folder");
            }
        }

        [HttpGet("root")]
        public async Task<IActionResult> GetRootFolders()
        {
            var (folders, files) = await _folderService.GetRootContentsAsync();

            return OkResponse(new { folders, files });
        }
    }
}
