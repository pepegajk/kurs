using System.ComponentModel.DataAnnotations;

namespace kursecondapi.DTOs;

public class CreateCarDto
{
    [Required]
    public int ModelId { get; set; }

    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    [Required]
    [StringLength(50)]
    public string Color { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string BodyType { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string FuelType { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Transmission { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string DriveType { get; set; } = null!;

    public decimal? EngineVolume { get; set; }

    public int? EnginePower { get; set; }

    [Required]
    [StringLength(17)]
    public string Vin { get; set; } = null!;

    [StringLength(20)]
    public string? RegistrationNumber { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Condition { get; set; } = null!;

    public bool IsFeatured { get; set; } = false;
}
