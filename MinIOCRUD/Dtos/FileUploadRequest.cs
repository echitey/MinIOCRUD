using System.ComponentModel.DataAnnotations;

namespace MinIOCRUD.Dtos
{
    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
