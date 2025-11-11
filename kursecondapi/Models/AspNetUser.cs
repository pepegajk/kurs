using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace kursecondapi.Models;

public partial class AspNetUser : IdentityUser<string>
{
    // Дополнительные свойства (базовые свойства уже в IdentityUser)
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Avatar { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    [JsonIgnore]
    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    [JsonIgnore]
    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    [JsonIgnore]
    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();

    [JsonIgnore]
    public virtual ICollection<Deal> DealApprovedByNavigations { get; set; } = new List<Deal>();

    [JsonIgnore]
    public virtual ICollection<Deal> DealBuyers { get; set; } = new List<Deal>();

    [JsonIgnore]
    public virtual ICollection<Deal> DealSellers { get; set; } = new List<Deal>();

    [JsonIgnore]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [JsonIgnore]
    public virtual ICollection<Review> ReviewAuthors { get; set; } = new List<Review>();

    [JsonIgnore]
    public virtual ICollection<Review> ReviewModeratedByNavigations { get; set; } = new List<Review>();

    [JsonIgnore]
    public virtual UserSetting? UserSetting { get; set; }

    [JsonIgnore]
    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
