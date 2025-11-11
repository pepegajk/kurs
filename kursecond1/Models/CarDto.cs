namespace kursecond1.Models;

public class CarDto
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string Color { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public decimal? EngineVolume { get; set; }
    public int? EnginePower { get; set; }
    public string VIN { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Condition { get; set; } = "Used";
    public int ViewsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsFeatured { get; set; }
    
    // Навигационные свойства
    public string? ModelName { get; set; }
    public string? BrandName { get; set; }
}

