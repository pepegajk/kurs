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
    // Поддержка обоих вариантов: строка "2025-06" или объект с year/month
    public string? Month { get; set; }
    public int? Year { get; set; }
    public int? MonthNumber { get; set; }
    
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    
    // Свойство для получения месяца в строковом формате
    public string MonthFormatted
    {
        get
        {
            if (!string.IsNullOrEmpty(Month))
                return Month;
            
            if (Year.HasValue && MonthNumber.HasValue)
                return $"{Year}-{MonthNumber:00}";
            
            return string.Empty;
        }
    }
}

public class PriceRangeDto
{
    public string Range { get; set; } = string.Empty;
    public int Count { get; set; }
}

