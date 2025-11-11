using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kursecondapi.Models;

public partial class Deal
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string SellerId { get; set; } = null!;

    public string BuyerId { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal? CommissionAmount { get; set; }

    public decimal? CommissionPercent { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ApprovedBy { get; set; }

    [JsonIgnore]
    public virtual AspNetUser? ApprovedByNavigation { get; set; }

    [JsonIgnore]
    public virtual AspNetUser Buyer { get; set; } = null!;

    [JsonIgnore]
    public virtual Car Car { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [JsonIgnore]
    public virtual AspNetUser Seller { get; set; } = null!;
}
