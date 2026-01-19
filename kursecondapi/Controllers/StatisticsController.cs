using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using System.Security.Claims;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ManagerOnly")]
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
            // Диагностическое логирование для проверки авторизации
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            _logger.LogWarning("GetStatistics вызван пользователем {UserId} с ролями: {Roles}. Все claims: {AllClaims}", 
                userId, string.Join(", ", userRoles), string.Join("; ", allClaims));
            
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

            // Вычисляем DealsByMonth - группировка завершённых сделок по месяцам
            // Используем CompletedAt если есть, иначе CreatedAt
            var completedDealsList = await _context.Deals
                .Where(d => d.Status == "Completed")
                .ToListAsync();
            
            var dealsByMonth = completedDealsList
                .Select(d => new
                {
                    Deal = d,
                    Date = d.CompletedAt ?? d.CreatedAt
                })
                .GroupBy(x => new { x.Date.Year, x.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.Deal.Price)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new
                {
                    Month = $"{x.Year}-{x.Month:00}",
                    x.Count,
                    x.Revenue
                })
                .ToList();

            // Вычисляем CarsByPriceRange - распределение по ценовым диапазонам
            var carsByPriceRange = await _context.Cars
                .GroupBy(c => c.Price < 1000000 ? "<1м" :
                              c.Price < 2000000 ? "1-2м" :
                              c.Price < 3000000 ? "2-3м" :
                              c.Price < 5000000 ? "3-5м" : "5м+")
                .Select(g => new
                {
                    Range = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Range)
                .ToListAsync();

            var statistics = new
            {
                TotalCars = totalCars,
                ActiveCars = activeCars,
                TotalDeals = totalDeals,
                CompletedDeals = completedDeals,
                TotalRevenue = totalRevenue,
                AverageCarPrice = totalCars > 0 ? await _context.Cars.AverageAsync(c => c.Price) : 0,
                TopBrands = await GetTopBrands(),
                DealsByMonth = dealsByMonth,
                CarsByPriceRange = carsByPriceRange
            };

            // Логирование для отладки
            _logger.LogInformation("Статистика: TotalCars={TotalCars}, ActiveCars={ActiveCars}, TotalDeals={TotalDeals}, CompletedDeals={CompletedDeals}, DealsByMonthCount={DealsByMonthCount}", 
                totalCars, activeCars, totalDeals, completedDeals, dealsByMonth.Count);

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

