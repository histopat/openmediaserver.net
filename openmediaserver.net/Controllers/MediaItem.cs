namespace openmediaserver.net.Controllers
{
    public class MediaItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public MediaType Type { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted => FileSizeBytes switch
        {
            < 1024 => $"{FileSizeBytes} B",
            < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
            _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
        };
        public DateTime LastModified { get; set; }
    }

    public enum MediaType
    {
        Video,
        Audio
    }
}
