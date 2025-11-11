using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class AspNetUserToken
{
    public string UserId { get; set; } = null!;

    public string LoginProvider { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    [JsonIgnore]
    public virtual AspNetUser User { get; set; } = null!;
}
