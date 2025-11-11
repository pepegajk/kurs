namespace kursecond1.Models;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

