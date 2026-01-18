using Microsoft.AspNetCore.Http;

namespace kursecondapi.DTOs;

public class UploadCarImageDto
{
    public IFormFile File { get; set; } = null!;
    public int CarId { get; set; }
}

