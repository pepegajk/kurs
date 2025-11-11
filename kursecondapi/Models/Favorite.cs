using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Favorite
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int CarId { get; set; }

    public DateTime AddedAt { get; set; }

    public string? Notes { get; set; }

    [JsonIgnore]
    public virtual Car Car { get; set; } = null!;

    [JsonIgnore]
    public virtual AspNetUser User { get; set; } = null!;
}
