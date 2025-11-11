namespace kursecond1.Models;

public class DealDto
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? CommissionPercent { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ApprovedBy { get; set; }
    
    // Навигационные свойства
    public string? CarInfo { get; set; }
    public string? SellerName { get; set; }
    public string? BuyerName { get; set; }
}

