namespace kursecond1.Models;

public class StatisticsDto
{
    public int TotalCars { get; set; }
    public int ActiveCars { get; set; }
    public int TotalDeals { get; set; }
    public int CompletedDeals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageCarPrice { get; set; }
    public List<BrandStatDto> TopBrands { get; set; } = new();
    public List<DealByMonthDto> DealsByMonth { get; set; } = new();
    public List<PriceRangeDto> CarsByPriceRange { get; set; } = new();
}

public class BrandStatDto
{
    public string BrandName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DealByMonthDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class PriceRangeDto
{
    public string Range { get; set; } = string.Empty;
    public int Count { get; set; }
}

