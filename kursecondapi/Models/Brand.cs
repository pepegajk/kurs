using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string? Country { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual ICollection<Model> Models { get; set; } = new List<Model>();
}
