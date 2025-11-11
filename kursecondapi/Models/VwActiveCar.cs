using System;
using System.Collections.Generic;

namespace kursecondapi.Models;

public partial class VwActiveCar
{
    public int? Id { get; set; }

    public int? ModelId { get; set; }

    public string? ModelName { get; set; }

    public string? BrandName { get; set; }

    public int? Year { get; set; }

    public decimal? Price { get; set; }

    public int? Mileage { get; set; }

    public string? FuelType { get; set; }

    public string? Transmission { get; set; }

    public string? DriveType { get; set; }

    public decimal? EngineVolume { get; set; }

    public int? EnginePower { get; set; }

    public string? Color { get; set; }

    public string? BodyType { get; set; }

    public string? Vin { get; set; }

    public string? Location { get; set; }

    public string? Condition { get; set; }

    public string? Status { get; set; }

    public bool? IsFeatured { get; set; }

    public int? ViewsCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? SellerId { get; set; }

    public string? SellerFirstName { get; set; }

    public string? SellerLastName { get; set; }

    public string? SellerEmail { get; set; }
}
