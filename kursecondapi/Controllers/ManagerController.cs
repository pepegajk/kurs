using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using System.Text;
using System.Globalization;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ManagerOnly")]
public class ManagerController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<ManagerController> _logger;

    public ManagerController(CarPlatformContext context, ILogger<ManagerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/manager/stats/views - График просмотров
    [HttpGet("stats/views")]
    public async Task<ActionResult> GetViewsStatistics([FromQuery] int? carId, [FromQuery] int days = 30)
    {
        try
        {
            var fromDate = DateTime.Now.AddDays(-days);
            
            if (carId.HasValue)
            {
                // Статистика для конкретного авто
                var car = await _context.Cars
                    .Where(c => c.Id == carId.Value)
                    .Select(c => new
                    {
                        CarId = c.Id,
                        Brand = c.Model.Brand.Name,
                        Model = c.Model.Name,
                        Year = c.Year,
                        ViewsCount = c.ViewsCount,
                        CreatedAt = c.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (car == null)
                    return NotFound(new { message = "Автомобиль не найден" });

                return Ok(new
                {
                    CarInfo = car,
                    Message = "Детальная статистика просмотров (требует доработки для хранения истории просмотров)"
                });
            }
            else
            {
                // Общая статистика по всем авто
                var topCars = await _context.Cars
                    .OrderByDescending(c => c.ViewsCount)
                    .Take(10)
                    .Select(c => new
                    {
                        CarId = c.Id,
                        Brand = c.Model.Brand.Name,
                        Model = c.Model.Name,
                        Year = c.Year,
                        ViewsCount = c.ViewsCount,
                        Price = c.Price
                    })
                    .ToListAsync();

                var totalViews = await _context.Cars.SumAsync(c => c.ViewsCount);

                return Ok(new
                {
                    TotalViews = totalViews,
                    TopViewedCars = topCars
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики просмотров");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/manager/stats/sales - График продаж
    [HttpGet("stats/sales")]
    public async Task<ActionResult> GetSalesStatistics([FromQuery] int days = 30)
    {
        try
        {
            var fromDate = DateTime.Now.AddDays(-days);
            
            var completedDeals = await _context.Deals
                .Where(d => d.Status == "Completed" && d.CompletedAt >= fromDate)
                .GroupBy(d => d.CompletedAt!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(d => d.Price),
                    Commission = g.Sum(d => d.CommissionAmount ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalSales = await _context.Deals
                .Where(d => d.Status == "Completed")
                .SumAsync(d => d.Price);

            var totalCommission = await _context.Deals
                .Where(d => d.Status == "Completed")
                .SumAsync(d => d.CommissionAmount ?? 0);

            var completedCount = await _context.Deals
                .CountAsync(d => d.Status == "Completed");

            return Ok(new
            {
                Period = $"Последние {days} дней",
                TotalSales = totalSales,
                TotalCommission = totalCommission,
                CompletedDealsCount = completedCount,
                DailySales = completedDeals
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики продаж");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/manager/stats/popular-brands - Популярные бренды
    [HttpGet("stats/popular-brands")]
    public async Task<ActionResult> GetPopularBrands()
    {
        try
        {
            var popularBrands = await _context.Cars
                .GroupBy(c => new { c.Model.BrandId, BrandName = c.Model.Brand.Name })
                .Select(g => new
                {
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.BrandName,
                    CarsCount = g.Count(),
                    TotalViews = g.Sum(c => c.ViewsCount),
                    AveragePrice = g.Average(c => c.Price)
                })
                .OrderByDescending(x => x.CarsCount)
                .Take(10)
                .ToListAsync();

            return Ok(popularBrands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении популярных брендов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/manager/export/cars - Экспорт автомобилей в CSV
    [HttpGet("export/cars")]
    public async Task<IActionResult> ExportCarsToCsv([FromQuery] string? status)
    {
        try
        {
            var query = _context.Cars.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var cars = await query
                .Select(c => new
                {
                    c.Id,
                    Brand = c.Model.Brand.Name,
                    Model = c.Model.Name,
                    c.Year,
                    c.Price,
                    c.Mileage,
                    c.Color,
                    c.BodyType,
                    c.FuelType,
                    c.Transmission,
                    c.Location,
                    c.Status,
                    c.ViewsCount,
                    c.CreatedAt
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Бренд,Модель,Год,Цена,Пробег,Цвет,Тип кузова,Топливо,Коробка,Город,Статус,Просмотры,Дата создания");

            foreach (var car in cars)
            {
                csv.AppendLine($"{car.Id},{car.Brand},{car.Model},{car.Year},{car.Price},{car.Mileage}," +
                             $"{car.Color},{car.BodyType},{car.FuelType},{car.Transmission}," +
                             $"{car.Location},{car.Status},{car.ViewsCount}," +
                             $"{car.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"cars_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте в CSV");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/manager/export/deals - Экспорт сделок в CSV
    [HttpGet("export/deals")]
    public async Task<IActionResult> ExportDealsToCsv([FromQuery] string? status)
    {
        try
        {
            var query = _context.Deals.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            var deals = await query
                .Select(d => new
                {
                    d.Id,
                    CarId = d.CarId,
                    d.Price,
                    d.CommissionAmount,
                    d.CommissionPercent,
                    d.Status,
                    d.CreatedAt,
                    CompletedAt = d.CompletedAt
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,ID Авто,Цена,Комиссия,Процент комиссии,Статус,Дата создания,Дата завершения");

            foreach (var deal in deals)
            {
                csv.AppendLine($"{deal.Id},{deal.CarId},{deal.Price},{deal.CommissionAmount ?? 0}," +
                             $"{deal.CommissionPercent ?? 0},{deal.Status}," +
                             $"{deal.CreatedAt:yyyy-MM-dd HH:mm}," +
                             $"{(deal.CompletedAt.HasValue ? deal.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm") : "N/A")}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"deals_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте сделок в CSV");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/manager/dashboard - Панель менеджера
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetManagerDashboard()
    {
        try
        {
            var totalCars = await _context.Cars.CountAsync();
            var activeCars = await _context.Cars.CountAsync(c => c.Status == "Active");
            var pendingDeals = await _context.Deals.CountAsync(d => d.Status == "Pending");
            var completedDeals = await _context.Deals.CountAsync(d => d.Status == "Completed");
            var totalViews = await _context.Cars.SumAsync(c => c.ViewsCount);

            var recentCars = await _context.Cars
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new
                {
                    c.Id,
                    Brand = c.Model.Brand.Name,
                    Model = c.Model.Name,
                    c.Year,
                    c.Price,
                    c.Status,
                    c.ViewsCount
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCars = totalCars,
                ActiveCars = activeCars,
                PendingDeals = pendingDeals,
                CompletedDeals = completedDeals,
                TotalViews = totalViews,
                RecentCars = recentCars
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении панели менеджера");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

