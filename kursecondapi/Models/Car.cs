using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Car
{
    public int Id { get; set; }

    public int ModelId { get; set; }

    public string SellerId { get; set; } = null!;

    public int Year { get; set; }

    public decimal Price { get; set; }

    public int Mileage { get; set; }

    public string Color { get; set; } = null!;

    public string BodyType { get; set; } = null!;

    public string FuelType { get; set; } = null!;

    public string Transmission { get; set; } = null!;

    public string DriveType { get; set; } = null!;

    public decimal? EngineVolume { get; set; }

    public int? EnginePower { get; set; }

    public string Vin { get; set; } = null!;

    public string? RegistrationNumber { get; set; }

    public string? Description { get; set; }

    public string Location { get; set; } = null!;

    public string Condition { get; set; } = null!;

    public int ViewsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Status { get; set; } = null!;

    public bool IsFeatured { get; set; }

    [JsonIgnore]
    public virtual ICollection<CarImage> CarImages { get; set; } = new List<CarImage>();

    [JsonIgnore]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    [JsonIgnore]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [JsonIgnore]
    public virtual Model Model { get; set; } = null!;

    [JsonIgnore]
    public virtual AspNetUser Seller { get; set; } = null!;
}
