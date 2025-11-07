using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos.Responses;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Services;

namespace MinIOCRUD.Controllers
{
    /// <summary>
    /// Handles folder creation, retrieval, and deletion within the MinIO storage system.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FoldersController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly IFolderService _folderService;

        public FoldersController(AppDbContext db, IFolderService folderService)
        {
            _db = db;
            _folderService = folderService;
        }

        /// <summary>
        /// Creates a new folder in the DB.
        /// </summary>
        /// <remarks>
        /// Example request:
        ///
        ///     POST /api/folders?name=Projects&amp;parentId=1f9c5b89-62f2-4d2f-8b20-45e4828e6a4b
        ///
        /// </remarks>
        /// <param name="name">The name of the new folder.</param>
        /// <param name="parentId">Optional parent folder ID if creating a subfolder.</param>
        /// <returns>The newly created folder.</returns>
        /// <response code="201">Folder created successfully.</response>
        /// <response code="400">Folder name is missing or creation failed.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFolder([FromQuery] string name, [FromQuery] Guid? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ErrorResponse("Folder name is required", 400);

            var folder = new Folder { Name = name, ParentId = parentId };
            await _folderService.CreateFolderAsync(folder);

            return folder.Id == Guid.Empty
                ? ErrorResponse("Folder creation failed", 400)
                : CreatedResponse(folder.ToDtoWithBreadcrumb(new List<BreadcrumbItemDto>()));
        }

        /// <summary>
        /// Retrieves folder details by ID, including breadcrumb information.
        /// </summary>
        /// <param name="id">The unique identifier of the folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The folder and its breadcrumb structure.</returns>
        /// <response code="200">Folder found and returned successfully.</response>
        /// <response code="404">Folder not found.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFolder(Guid id, CancellationToken cancellationToken = default)
        {
            var folder = await _folderService.GetFolderDtoWithBreadcrumbsAsync(id, cancellationToken);

            return folder == null
                ? ErrorResponse("Folder not found", 404)
                : OkResponse(folder);
        }

        /// <summary>
        /// Deletes a folder and all its contained files/subfolders.
        /// </summary>
        /// <param name="id">The unique identifier of the folder to delete.</param>
        /// <returns>No content if deletion succeeds.</returns>
        /// <response code="204">Folder deleted successfully.</response>
        /// <response code="400">An error occurred during deletion.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves all root-level folders and files (those without a parent).
        /// </summary>
        /// <returns>List of root folders and files.</returns>
        /// <response code="200">Root folders and files returned successfully.</response>
        [HttpGet("root")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRootFolders()
        {
            var (folders, files) = await _folderService.GetRootContentsAsync();

            return OkResponse(new { folders, files });
        }
    }
}
