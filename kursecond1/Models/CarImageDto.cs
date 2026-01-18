namespace kursecond1.Models;

public class CarImageDto
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }
    public DateTime UploadedAt { get; set; }
}

