namespace openmediaserver.net.Controllers
{
    public class MediaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MediaService> _logger;

        private static readonly Dictionary<string, (string ContentType, MediaType MediaType)> SupportedFormats =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { ".mp4", ("video/mp4", MediaType.Video) },
                { ".mkv", ("video/x-matroska", MediaType.Video) },
                { ".avi", ("video/x-msvideo", MediaType.Video) },
                { ".mp3", ("audio/mpeg", MediaType.Audio) },
                { ".flac", ("audio/flac", MediaType.Audio) },
                { ".wav", ("audio/wav", MediaType.Audio) },
                { ".wmv", ("video/x-ms-wmv", MediaType.Video) },
                { ".wma", ("audio/x-ms-wma", MediaType.Audio) },
            };

        public MediaService(IConfiguration configuration, ILogger<MediaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string MediaRootPath =>
            _configuration["MediaSettings:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "MediaLibrary");

        public List<MediaItem> GetAllMedia()
        {
            var items = new List<MediaItem>();
            var rootPath = MediaRootPath;

            if (!Directory.Exists(rootPath))
            {
                _logger.LogWarning("Medya dizini bulunamadı: {Path}", rootPath);
                Directory.CreateDirectory(rootPath);
                return items;
            }

            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (!SupportedFormats.TryGetValue(ext, out var format))
                    continue;

                var fileInfo = new FileInfo(file);
                var relativePath = Path.GetRelativePath(rootPath, file);
                var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(relativePath))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                items.Add(new MediaItem
                {
                    Id = id,
                    Title = Path.GetFileNameWithoutExtension(file),
                    FileName = fileInfo.Name,
                    FilePath = relativePath,
                    ContentType = format.ContentType,
                    Type = format.MediaType,
                    FileSizeBytes = fileInfo.Length,
                });
            }

            return items.OrderBy(x => x.Type).ThenBy(x => x.Title).ToList();
        }

        public MediaItem? GetById(string id)
        {
            var all = GetAllMedia();
            return all.FirstOrDefault(x => x.Id == id);
        }

        public (string FilePath, string ContentType)? ResolveFile(string id)
        {
            var item = GetById(id);
            if (item is null) return null;

            var fullPath = Path.Combine(MediaRootPath, item.FilePath);
            if (!File.Exists(fullPath)) return null;

            return (fullPath, item.ContentType);
        }

        public (string FilePath, string ContentType)? ResolveFileByName(string filename)
        {
            var rootPath = MediaRootPath;
            if (!Directory.Exists(rootPath)) return null;

            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            var file = files.FirstOrDefault(f =>
                Path.GetFileName(f).Equals(filename, StringComparison.OrdinalIgnoreCase));

            if (file is null || !File.Exists(file)) return null;

            var ext = Path.GetExtension(file);
            if (!SupportedFormats.TryGetValue(ext, out var format)) return null;

            return (file, format.ContentType);
        }
    }
}
