using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class CarImage
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Title { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsMain { get; set; }

    public DateTime UploadedAt { get; set; }

    [JsonIgnore]
    public virtual Car Car { get; set; } = null!;
}
