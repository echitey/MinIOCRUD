namespace MinIOCRUD.Utils
{
    public static class Constants
    {

        public static string ToFriendlyName(SimpleFileType fileType)
        {
            return fileType switch
            {
                SimpleFileType.Pdf => "PDF",
                SimpleFileType.Word => "Word Document",
                SimpleFileType.Excel => "Excel Spreadsheet",
                SimpleFileType.Ppt => "Powerpoint Document",
                SimpleFileType.Image => "Image",
                SimpleFileType.Text => "Text File",
                SimpleFileType.Csv => "CSV File",
                SimpleFileType.Zip => "ZIP Archive",
                SimpleFileType.Video => "Video",
                SimpleFileType.Audio => "Audio",
                _ => "Unknown"
            };
        }


        public static readonly Dictionary<SimpleFileType, string> SimpleToDefaultMime = new()
        {
            { SimpleFileType.Pdf, "application/pdf" },
            { SimpleFileType.Word, "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { SimpleFileType.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { SimpleFileType.Ppt, "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { SimpleFileType.Text, "text/plain" },
            { SimpleFileType.Zip, "application/zip" },

            { SimpleFileType.Image, "image/jpeg" }, // choose JPEG as safe default
            { SimpleFileType.Audio, "audio/mpeg" }, // choose MP3 as safe default
            { SimpleFileType.Video, "video/mp4" },  // choose MP4 as safe default

            { SimpleFileType.Unknown, "application/octet-stream" }
        };


        public static readonly Dictionary<string, SimpleFileType> MimeToSimple = new(StringComparer.OrdinalIgnoreCase)
        {
            // 📄 Documents
            { "application/pdf", SimpleFileType.Pdf },
            { "application/msword", SimpleFileType.Word },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", SimpleFileType.Word },
            { "application/vnd.ms-excel", SimpleFileType.Excel },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SimpleFileType.Excel },
            { "application/vnd.ms-powerpoint", SimpleFileType.Ppt },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", SimpleFileType.Ppt },
            { "text/plain", SimpleFileType.Text },

            // 🗜 Archives
            { "application/zip", SimpleFileType.Zip },
            { "application/x-zip-compressed", SimpleFileType.Zip },
            { "application/x-rar-compressed", SimpleFileType.Zip },
            { "application/x-7z-compressed", SimpleFileType.Zip },
            { "application/gzip", SimpleFileType.Zip },

            // 🖼 Images
            { "image/jpeg", SimpleFileType.Image },
            { "image/jpg", SimpleFileType.Image }, // alias foo jpeg
            { "image/png", SimpleFileType.Image },
            { "image/gif", SimpleFileType.Image },
            { "image/bmp", SimpleFileType.Image },
            { "image/webp", SimpleFileType.Image },
            { "image/tiff", SimpleFileType.Image },
            { "image/heif", SimpleFileType.Image },
            { "image/heic", SimpleFileType.Image },
            { "image/svg+xml", SimpleFileType.Image },
            { "image/x-icon", SimpleFileType.Image }, // .ico

            // 🔊 Audio
            { "audio/mpeg", SimpleFileType.Audio },  // .mp3
            { "audio/mp3", SimpleFileType.Audio },   // alias
            { "audio/wav", SimpleFileType.Audio },
            { "audio/x-wav", SimpleFileType.Audio },
            { "audio/ogg", SimpleFileType.Audio },
            { "audio/opus", SimpleFileType.Audio },
            { "audio/aac", SimpleFileType.Audio },
            { "audio/m4a", SimpleFileType.Audio },
            { "audio/webm", SimpleFileType.Audio },
            { "audio/flac", SimpleFileType.Audio },

            // 🎬 Video
            { "video/mp4", SimpleFileType.Video },
            { "video/x-m4v", SimpleFileType.Video },
            { "video/webm", SimpleFileType.Video },
            { "video/ogg", SimpleFileType.Video },   // .ogv
            { "video/quicktime", SimpleFileType.Video }, // .mov
            { "video/x-msvideo", SimpleFileType.Video }, // .avi
            { "video/x-ms-wmv", SimpleFileType.Video },
            { "video/3gpp", SimpleFileType.Video },
            { "video/3gpp2", SimpleFileType.Video },
            { "video/mpeg", SimpleFileType.Video },
            { "video/mp2t", SimpleFileType.Video }
        };

        public static readonly Dictionary<string, SimpleFileType> ExtToSimple = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", SimpleFileType.Image },
            { ".jpeg", SimpleFileType.Image },
            { ".png", SimpleFileType.Image },
            { ".gif", SimpleFileType.Image },
            { ".bmp", SimpleFileType.Image },
            { ".webp", SimpleFileType.Image },
            { ".tiff", SimpleFileType.Image },
            { ".heic", SimpleFileType.Image },
            { ".heif", SimpleFileType.Image },
            { ".svg", SimpleFileType.Image },
            { ".ico", SimpleFileType.Image },

            // Audio
            { ".mp3", SimpleFileType.Audio },
            { ".wav", SimpleFileType.Audio },
            { ".ogg", SimpleFileType.Audio },
            { ".opus", SimpleFileType.Audio },
            { ".aac", SimpleFileType.Audio },
            { ".m4a", SimpleFileType.Audio },
            { ".flac", SimpleFileType.Audio },

            // Video
            { ".mp4", SimpleFileType.Video },
            { ".m4v", SimpleFileType.Video },
            { ".webm", SimpleFileType.Video },
            { ".ogv", SimpleFileType.Video },
            { ".mov", SimpleFileType.Video },
            { ".avi", SimpleFileType.Video },
            { ".wmv", SimpleFileType.Video },
            { ".3gp", SimpleFileType.Video },
            { ".3g2", SimpleFileType.Video },
            { ".mpeg", SimpleFileType.Video },
            { ".ts", SimpleFileType.Video },

            // Docs / Archives
            { ".pdf", SimpleFileType.Pdf },
            { ".doc", SimpleFileType.Word },
            { ".docx", SimpleFileType.Word },
            { ".xls", SimpleFileType.Excel },
            { ".xlsx", SimpleFileType.Excel },
            { ".ppt", SimpleFileType.Ppt },
            { ".pptx", SimpleFileType.Ppt },
            { ".txt", SimpleFileType.Text },
            { ".zip", SimpleFileType.Zip },
            { ".rar", SimpleFileType.Zip },
            { ".7z", SimpleFileType.Zip },
            { ".gz", SimpleFileType.Zip }
        };

        public static readonly Dictionary<string, string> ExtToMime = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
            { ".tiff", "image/tiff" },
            { ".heic", "image/heic" },
            { ".heif", "image/heif" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },

            // Audio
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".opus", "audio/opus" },
            { ".aac", "audio/aac" },
            { ".m4a", "audio/mp4" },
            { ".flac", "audio/flac" },

            // Video
            { ".mp4", "video/mp4" },
            { ".m4v", "video/x-m4v" },
            { ".webm", "video/webm" },
            { ".ogv", "video/ogg" },
            { ".mov", "video/quicktime" },
            { ".avi", "video/x-msvideo" },
            { ".wmv", "video/x-ms-wmv" },
            { ".3gp", "video/3gpp" },
            { ".3g2", "video/3gpp2" },
            { ".mpeg", "video/mpeg" },
            { ".ts", "video/mp2t" },

            // Docs / Archives
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".txt", "text/plain" },
            { ".zip", "application/zip" },
            { ".rar", "application/vnd.rar" },
            { ".7z", "application/x-7z-compressed" },
            { ".gz", "application/gzip" }
        };
    }
}
