using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Dtos.Requests
{
    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
