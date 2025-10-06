using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Services;
using MinIOCRUD.Utils;
using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request, Guid? folderId = null)
        {
            var result = await _fileService.UploadAsync(request, folderId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var files = await _fileService.ListAsync(page, pageSize);
            return Ok(files);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _fileService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var url = await _fileService.GetDownloadUrlAsync(id);
            return Redirect(url);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _fileService.SoftDeleteAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}/hard")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            await _fileService.HardDeleteAsync(id);
            return NoContent();
        }

        [HttpPost("hard-delete")]
        public async Task<IActionResult> HardDeleteBulk([FromBody] HardDeleteRequest request)
        {
            await _fileService.HardDeleteBulkAsync(request);
            return NoContent();
        }

        [HttpPost("presign-upload")]
        public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignUploadRequest request)
        {
            var result = await _fileService.GetPresignedUploadUrlAsync(request);
            return Ok(result);
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmUpload(Guid id)
        {
            var result = await _fileService.ConfirmUploadAsync(id);
            return Ok(result);
        }
    }
}
