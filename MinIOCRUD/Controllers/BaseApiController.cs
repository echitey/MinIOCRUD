using Microsoft.AspNetCore.Mvc;
using MinIOCRUD.Dtos.Responses;

namespace MinIOCRUD.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult OkResponse<T>(T data, string? message = null)
        {
            return Ok(ApiResponse<T>.Ok(data, message));
        }

        protected IActionResult CreatedResponse<T>(T data, string? message = null)
        {
            return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Created(data, message));
        }

        protected IActionResult ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return StatusCode(statusCode, ApiResponse<object>.Fail(message, statusCode, errors));
        }
    }
}
