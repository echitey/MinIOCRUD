using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Dtos.Requests;
using MinIOCRUD.Dtos.Responses;
using MinIOCRUD.Extensions;
using MinIOCRUD.Models;
using MinIOCRUD.Services;
using MinIOCRUD.Utils;
using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Controllers
{
    /// <summary>
    /// Manages file operations including upload, retrieval, deletion, and presigned URLs.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FilesController : BaseApiController
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Uploads a file to MinIO storage.
        /// </summary>
        /// <remarks>
        /// Example request:
        ///
        ///     POST /api/files
        ///     Content-Type: multipart/form-data
        ///
        ///     file: (binary)
        ///
        /// </remarks>
        /// <param name="request">Form-data containing the file and optional metadata.</param>
        /// <param name="folderId">Optional folder ID where the file should be stored.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Details of the uploaded file.</returns>
        /// <response code="201">File uploaded successfully.</response>
        /// <response code="500">File upload failed.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload(
            [FromForm] FileUploadRequest request,
            Guid? folderId = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _fileService.UploadAsync(request, folderId, cancellationToken);

            return result == null
                ? ErrorResponse("File upload failed", 500)
                : CreatedResponse(result);
        }

        /// <summary>
        /// Lists all files with pagination support.
        /// </summary>
        /// <param name="page">The current page number (default 1).</param>
        /// <param name="pageSize">Number of items per page (default 20).</param>
        /// <returns>Paged list of files.</returns>
        /// <response code="200">Files retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var files = await _fileService.ListAsync(page, pageSize);
            return OkResponse(files, "Files List");
        }

        /// <summary>
        /// Retrieves a specific file by its unique ID.
        /// </summary>
        /// <param name="id">The unique identifier of the file.</param>
        /// <returns>The file's metadata and details.</returns>
        /// <response code="200">File found.</response>
        /// <response code="404">File not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _fileService.GetByIdAsync(id);

            return result == null
                ? ErrorResponse("File not found", 404)
                : OkResponse(result);
        }

        /// <summary>
        /// Generates a temporary presigned URL for downloading the file. Make sure to set the right expire time in the env var.
        /// </summary>
        /// <param name="id">The unique identifier of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A time-limited URL for direct file download.</returns>
        /// <response code="200">Presigned download URL generated.</response>
        /// <response code="400">Failed to generate download URL.</response>
        [HttpGet("{id}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken cancellationToken = default)
        {
            var url = await _fileService.GetDownloadUrlAsync(id, cancellationToken);

            return string.IsNullOrEmpty(url)
                ? ErrorResponse("Could not generate download URL")
                : OkResponse(new DownloadUrlResponse(url), "File download URL");
        }

        /// <summary>
        /// Soft-deletes a file (marks it as deleted without removing from storage).
        /// </summary>
        /// <param name="id">The unique identifier of the file to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">File soft-deleted successfully.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken = default)
        {
            await _fileService.SoftDeleteAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a file from the storage.
        /// </summary>
        /// <param name="id">The unique identifier of the file to permanently delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">File permanently deleted.</response>
        [HttpDelete("{id}/hard")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> HardDelete(Guid id, CancellationToken cancellationToken = default)
        {
            await _fileService.HardDeleteAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes multiple files based on a request body.
        /// </summary>
        /// <param name="request">List of file IDs to delete and force delete flag</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Files permanently deleted.</response>
        [HttpPost("hard-delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> HardDeleteBulk([FromBody] HardDeleteRequest request, CancellationToken cancellationToken = default)
        {
            await _fileService.HardDeleteBulkAsync(request, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Generates a presigned upload URL for direct client-side upload to MinIO.
        /// </summary>
        /// <param name="request">File metadata used to generate the upload URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Presigned URL and related metadata.</returns>
        /// <response code="200">Presigned upload URL generated successfully.</response>
        /// <response code="400">Failed to generate upload URL.</response>
        [HttpPost("presign-upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignUploadRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _fileService.GetPresignedUploadUrlAsync(request, cancellationToken);

            return result == null
                ? ErrorResponse("Could not generate presigned upload URL")
                : OkResponse(result);
        }

        /// <summary>
        /// Confirms that an uploaded file was successfully processed.
        /// </summary>
        /// <param name="id">The unique identifier of the uploaded file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The confirmed file metadata.</returns>
        /// <response code="200">Upload confirmed successfully.</response>
        /// <response code="400">Upload confirmation failed.</response>
        [HttpPost("{id}/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmUpload(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _fileService.ConfirmUploadAsync(id, cancellationToken);

            return result == null
                ? ErrorResponse("Could not confirm upload")
                : OkResponse(result);
        }
    }
}
