using Microsoft.AspNetCore.Http;

namespace YessBackend.Application.DTOs.Uploads;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = default!;
    public string? Folder { get; set; }
}
