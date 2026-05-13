using openmediaserver.net.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MediaService>();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// -- Medya Listesi
app.MapGet("/api/media", (MediaService svc) =>
{
    var items = svc.GetAllMedia();
    return Results.Ok(items);
})
.WithName("GetMediaList");

// -- Tekil Medya Bilgisi
app.MapGet("/api/media/{id}", (string id, MediaService svc) =>
{
    var item = svc.GetById(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
.WithName("GetMediaItem");

// — Medya Akışı (Range destekli)
app.MapGet("/api/stream/{id}", static async (string id, HttpContext ctx, MediaService svc) =>
{
    var resolved = svc.ResolveFile(id);
    if (resolved is null)
    {
        ctx.Response.StatusCode = 404;
        return;
    }

    var fileTuple = resolved.Value;
    var filePath = fileTuple.Item1;
    var contentType = fileTuple.Item2;

    var fileInfo = new FileInfo(filePath);
    var totalLength = fileInfo.Length;

    ctx.Response.ContentType = contentType;
    ctx.Response.Headers.AcceptRanges = "bytes";

    var rangeHeader = ctx.Request.Headers.Range.ToString();

    if (string.IsNullOrEmpty(rangeHeader))
    {
        ctx.Response.ContentLength = totalLength;
        ctx.Response.StatusCode = 200;
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        await fs.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        return;
    }

    var range = ParseRange(rangeHeader, totalLength);
    if (range is null)
    {
        ctx.Response.StatusCode = 416;
        ctx.Response.Headers.ContentRange = $"bytes */{totalLength}";
        return;
    }

    var (start, end) = range.Value;
    var chunkLength = end - start + 1;

    ctx.Response.StatusCode = 206;
    ctx.Response.ContentLength = chunkLength;
    ctx.Response.Headers.ContentRange = $"bytes {start}-{end}/{totalLength}";

    await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
    stream.Seek(start, SeekOrigin.Begin);

    var buffer = new byte[65536];
    var remaining = chunkLength;

    while (remaining > 0 && !ctx.RequestAborted.IsCancellationRequested)
    {
        var toRead = (int)Math.Min(buffer.Length, remaining);
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, toRead), ctx.RequestAborted);
        if (bytesRead == 0) break;

        await ctx.Response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), ctx.RequestAborted);
        remaining -= bytesRead;
    }
})
.WithName("StreamMedia");

// Dosya Adı ile Medya Akışı
app.MapGet("/media/{filename}", async (string filename, HttpContext ctx, MediaService svc) =>
{
    var resolved = svc.ResolveFileByName(filename);
    if (resolved is null)
    {
        ctx.Response.StatusCode = 404;
        return;
    }

    var (filePath, contentType) = resolved.Value;
    var fileInfo = new FileInfo(filePath);
    var totalLength = fileInfo.Length;

    ctx.Response.ContentType = contentType;
    ctx.Response.Headers.AcceptRanges = "bytes";
    ctx.Response.Headers.ContentDisposition = $"inline; filename=\"{filename}\"";

    var rangeHeader = ctx.Request.Headers.Range.ToString();

    if (string.IsNullOrEmpty(rangeHeader))
    {
        ctx.Response.ContentLength = totalLength;
        ctx.Response.StatusCode = 200;
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        await fs.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        return;
    }

    var range = ParseRange(rangeHeader, totalLength);
    if (range is null)
    {
        ctx.Response.StatusCode = 416;
        ctx.Response.Headers.ContentRange = $"bytes */{totalLength}";
        return;
    }
});

static (long Start, long End)? ParseRange(string rangeHeader, long totalLength)
{
    if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        return null;

    var rangeValue = rangeHeader["bytes=".Length..];
    var parts = rangeValue.Split('-');
    if (parts.Length != 2) return null;

    long start, end;

    if (string.IsNullOrEmpty(parts[0]))
    {
        // bytes=-500 -> son 500 byte
        if (!long.TryParse(parts[1], out var suffix) || suffix <= 0) return null;
        start = totalLength - suffix;
        end = totalLength - 1;
    }
    else if (string.IsNullOrEmpty(parts[1]))
    {
        // bytes=500- -> 500'den sona kadar
        if (!long.TryParse(parts[0], out start)) return null;
        end = totalLength - 1;
    }
    else
    {
        if (!long.TryParse(parts[0], out start) || !long.TryParse(parts[1], out end)) return null;
    }

    if (start < 0 || end >= totalLength || start > end) return null;

    return (start, end);
}