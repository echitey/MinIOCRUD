namespace MinIOCRUD.Dtos.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }

        public static ApiResponse<T> Created(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "Resource created successfully",
                StatusCode = 201
            };
        }

        public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors
            };
        }

        public static ApiResponse<T> FromException(Exception ex, int statusCode = 500)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = ex.Message,
                StatusCode = statusCode,
                Errors = new List<string> { ex.InnerException?.Message ?? ex.Message }
            };
        }
    }
}
