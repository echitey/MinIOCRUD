using MinIOCRUD.Models;

namespace MinIOCRUD.Utils
{
    public static class FileTypeHelper
    {
        public static SimpleFileType ToSimpleFileType(string contentType, string? fileName = null)
        {

            if (!string.IsNullOrWhiteSpace(contentType)
                && Constants.MimeToSimple.TryGetValue(contentType, out var fromMime))
            {
                return fromMime;
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var ext = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(ext)
                    && Constants.ExtToSimple.TryGetValue(ext, out var fromExt))
                {
                    return fromExt;
                }
            }

            return SimpleFileType.Unknown;
        }

        public static string GetSafeContentType(string contentType, string fileName)
        {
            // If content type exists and isn’t a generic octet-stream → use it
            if (!string.IsNullOrEmpty(contentType) &&
                !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                return contentType;
            }

            // Try extension fallback
            var ext = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext) && Constants.ExtToMime.TryGetValue(ext, out var mimeFromExt))
            {
                return mimeFromExt;
            }

            // Fallback default
            var fileType = ToSimpleFileType(contentType, fileName);
            return ToDefaultContentType(fileType);
        }

        public static string ToDefaultContentType(SimpleFileType fileType)
        {
            return Constants.SimpleToDefaultMime.TryGetValue(fileType, out var mime)
                ? mime
                : "application/octet-stream";
        }

    }

}
