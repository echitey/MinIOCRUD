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
    public class FoldersController : ControllerBase
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
            var folder = new Folder { Name = name, ParentId = parentId };
            
            await _folderService.CreateFolderAsync(folder);

            return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder.ToDto());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFolder(Guid id)
        {
            var folder = await _folderService.GetFolderDtoWithBreadcrumbsAsync(id);

            if (folder == null)
                return NotFound();

            return Ok(folder);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFolder(Guid id)
        {
            try
            {
                await _folderService.DeleteFolderAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("root")]
        public async Task<IActionResult> GetRootFolders()
        {
            var (folders, files) = await _folderService.GetRootFoldersAsync();

            return Ok(new { folders, files });
        }
    }
}
