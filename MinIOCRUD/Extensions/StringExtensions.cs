using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MinIOCRUD.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Sanitizes a file name for safe use in both file systems and MinIO object storage.
        /// - Removes invalid path characters
        /// - Converts accented and non-ASCII characters to ASCII equivalents
        /// - Replaces spaces with underscores
        /// - Collapses multiple underscores
        /// - Ensures filename length and extension safety
        /// </summary>
        public static string SanitizeFileName(this string fileName, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";

            // Extract base name and extension safely
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            // Normalize to decompose accent marks, then remove them
            var normalized = nameWithoutExt.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            nameWithoutExt = sb.ToString().Normalize(NormalizationForm.FormC);

            // Remove invalid file name characters
            var invalidChars = Path.GetInvalidFileNameChars();
            nameWithoutExt = new string(nameWithoutExt.Where(c => !invalidChars.Contains(c)).ToArray());

            // Replace spaces and special chars with underscores
            nameWithoutExt = Regex.Replace(nameWithoutExt, @"[^a-zA-Z0-9_-]", "_");

            // Collapse multiple underscores
            nameWithoutExt = Regex.Replace(nameWithoutExt, "_{2,}", "_");

            // Trim underscores or dots
            nameWithoutExt = nameWithoutExt.Trim('_', '.');

            // Limit length
            if (nameWithoutExt.Length > maxLength)
                nameWithoutExt = nameWithoutExt[..maxLength];

            // Recombine and lower case
            var sanitized = $"{nameWithoutExt}{extension}".ToLowerInvariant();

            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
        }
    }
}
