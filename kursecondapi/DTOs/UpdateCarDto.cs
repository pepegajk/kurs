using System.ComponentModel.DataAnnotations;

namespace kursecondapi.DTOs;

public class UpdateCarDto
{
    public int? ModelId { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    [Range(0, int.MaxValue)]
    public int? Mileage { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? BodyType { get; set; }

    [StringLength(50)]
    public string? FuelType { get; set; }

    [StringLength(50)]
    public string? Transmission { get; set; }

    [StringLength(50)]
    public string? DriveType { get; set; }

    public decimal? EngineVolume { get; set; }

    public int? EnginePower { get; set; }

    [StringLength(17)]
    public string? Vin { get; set; }

    [StringLength(20)]
    public string? RegistrationNumber { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? Condition { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public bool? IsFeatured { get; set; }
}
