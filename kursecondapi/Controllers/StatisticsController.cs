using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class StatisticsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(CarPlatformContext context, ILogger<StatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/statistics
    [HttpGet]
    public async Task<ActionResult> GetStatistics()
    {
        try
        {
            var totalCars = await _context.Cars.CountAsync();
            var activeCars = await _context.Cars.CountAsync(c => c.Status == "Active");
            var soldCars = await _context.Cars.CountAsync(c => c.Status == "Sold");
            var totalDeals = await _context.Deals.CountAsync();
            var completedDeals = await _context.Deals.CountAsync(d => d.Status == "Completed");
            
            var totalRevenue = await _context.Deals
                .Where(d => d.Status == "Completed")
                .SumAsync(d => d.Price);

            var totalCommission = await _context.Deals
                .Where(d => d.Status == "Completed")
                .SumAsync(d => d.CommissionAmount ?? 0);

            var totalViews = await _context.Cars.SumAsync(c => c.ViewsCount);

            var statistics = new
            {
                TotalCars = totalCars,
                ActiveCars = activeCars,
                SoldCars = soldCars,
                TotalDeals = totalDeals,
                CompletedDeals = completedDeals,
                TotalRevenue = totalRevenue,
                TotalCommission = totalCommission,
                TotalViews = totalViews,
                AverageCarPrice = totalCars > 0 ? await _context.Cars.AverageAsync(c => c.Price) : 0,
                TopBrands = await GetTopBrands()
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/statistics/sales-by-month
    [HttpGet("sales-by-month")]
    public async Task<ActionResult> GetSalesByMonth([FromQuery] int months = 6)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            
            var salesByMonth = await _context.Deals
                .Where(d => d.CompletedAt >= startDate && d.Status == "Completed")
                .GroupBy(d => new { d.CompletedAt!.Value.Year, d.CompletedAt.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalRevenue = g.Sum(d => d.Price),
                    TotalCommission = g.Sum(d => d.CommissionAmount ?? 0)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return Ok(salesByMonth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении продаж по месяцам");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/statistics/car-views/{carId}
    [HttpGet("car-views/{carId}")]
    public async Task<ActionResult> GetCarViewsStatistics(int carId)
    {
        try
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
                return NotFound(new { message = $"Автомобиль с ID {carId} не найден" });

            // Возвращаем базовую статистику просмотров
            var statistics = new
            {
                CarId = car.Id,
                VIN = car.Vin,
                ViewsCount = car.ViewsCount,
                IsFeatured = car.IsFeatured,
                Status = car.Status,
                CreatedAt = car.CreatedAt,
                DaysOnMarket = (DateTime.UtcNow - car.CreatedAt).Days
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики просмотров автомобиля");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/statistics/top-cars
    [HttpGet("top-cars")]
    public async Task<ActionResult> GetTopViewedCars([FromQuery] int limit = 10)
    {
        try
        {
            var topCars = await _context.Cars
                .OrderByDescending(c => c.ViewsCount)
                .Take(limit)
                .Select(c => new
                {
                    c.Id,
                    VIN = c.Vin,
                    c.Year,
                    c.Price,
                    c.ViewsCount,
                    c.Status,
                    ModelName = c.Model != null ? c.Model.Name : "N/A",
                    BrandName = c.Model != null && c.Model.Brand != null ? c.Model.Brand.Name : "N/A"
                })
                .ToListAsync();

            return Ok(topCars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении топ автомобилей");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    private async Task<List<object>> GetTopBrands()
    {
        try
        {
            var topBrands = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m!.Brand)
                .Where(c => c.Model != null && c.Model.Brand != null)
                .GroupBy(c => new { c.Model!.Brand!.Id, c.Model.Brand.Name })
                .Select(g => new
                {
                    BrandId = g.Key.Id,
                    BrandName = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync<object>();

            return topBrands;
        }
        catch
        {
            return new List<object>();
        }
    }
}

