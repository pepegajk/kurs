using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace kursecondapi.Models;

public partial class AspNetRole : IdentityRole<string>
{
    // Базовые свойства уже в IdentityRole<string>
    [JsonIgnore]
    public virtual ICollection<AspNetRoleClaim> AspNetRoleClaims { get; set; } = new List<AspNetRoleClaim>();

    [JsonIgnore]
    public virtual ICollection<AspNetUser> Users { get; set; } = new List<AspNetUser>();
}
