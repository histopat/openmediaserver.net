# openmediaserver.net

**openmediaserver.net** is an open-source, simple, and scalable media streaming server developed using C# and running on the .NET platform. It supports operation on Windows, macOS, and Linux systems.

## How it works?

openmediaserver.net is a media server application developed to scan video (MP4, MKV, AVI, WMV) and audio (MP3, FLAC, WAV, WMA) files from a configurable media library folder (by default, the ***library*** folder is located in the application directory) and serve them to clients via a RESTful API. The application provides a collection API that allows querying media files by ID or filename, supports HTTP Range headers for back-and-forward (seek) operations, converts file sizes to human-readable format, and sorts and serves all media by type (video/audio); thus, web or mobile applications can access the entire media catalog with simple GET requests, retrieve individual media information, or request file streams within a desired byte range.

## System Requirements

* Windows, Linux or macOS
* A suitable Web server (hosting on IIS using ASP.NET Core Hosting Bundle on Windows Server is recommended)
* .NET 10 runtime
* **To compile:** .NET 10 SDK (Visual Studio 2026 recommended)

## License

This project is licensed under the MIT license.

