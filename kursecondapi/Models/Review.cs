using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Review
{
    public int Id { get; set; }

    public int DealId { get; set; }

    public string AuthorId { get; set; } = null!;

    public int Rating { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsApproved { get; set; }

    public string? ModeratedBy { get; set; }

    [JsonIgnore]
    public virtual AspNetUser Author { get; set; } = null!;

    [JsonIgnore]
    public virtual Deal Deal { get; set; } = null!;

    [JsonIgnore]
    public virtual AspNetUser? ModeratedByNavigation { get; set; }
}
