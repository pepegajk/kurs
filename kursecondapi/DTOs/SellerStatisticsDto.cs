namespace kursecondapi.DTOs;

public class SellerStatisticsDto
{
    public List<SellerStatisticItem> Sellers { get; set; } = new();
}

public class SellerStatisticItem
{
    public string SellerId { get; set; } = null!;
    public string SellerName { get; set; } = null!;
    public string? SellerEmail { get; set; }
    public int TotalCars { get; set; }
    public int ActiveCars { get; set; }
    public int PendingCars { get; set; }
    public int SoldCars { get; set; }
    public int TotalDeals { get; set; }
    public int CompletedDeals { get; set; }
    public int ActiveDeals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageCarPrice { get; set; }
    public double? AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
