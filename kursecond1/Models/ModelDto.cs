namespace kursecond1.Models;

public class ModelDto
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? BrandName { get; set; }
}

