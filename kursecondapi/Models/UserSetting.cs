using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class UserSetting
{
    public string UserId { get; set; } = null!;

    public string Theme { get; set; } = null!;

    public string Language { get; set; } = null!;

    public string DateFormat { get; set; } = null!;

    public string TimeFormat { get; set; } = null!;

    public string Currency { get; set; } = null!;

    public int PageSize { get; set; }

    public bool EmailNotifications { get; set; }

    public bool PushNotifications { get; set; }

    public string? SavedFilters { get; set; }

    public string? FavoriteLocations { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual AspNetUser User { get; set; } = null!;
}
