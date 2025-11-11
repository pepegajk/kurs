using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Model
{
    public int Id { get; set; }

    public int BrandId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? YearFrom { get; set; }

    public int? YearTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual Brand Brand { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
}
