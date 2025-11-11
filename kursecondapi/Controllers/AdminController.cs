using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using System.Security.Claims;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(CarPlatformContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/admin/reviews/pending - Получить неодобренные отзывы
    [HttpGet("reviews/pending")]
    public async Task<ActionResult<IEnumerable<Review>>> GetPendingReviews()
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении неодобренных отзывов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/admin/reviews/{id}/approve - Одобрить отзыв
    [HttpPut("reviews/{id}/approve")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            review.IsApproved = true;
            review.ModeratedBy = adminId;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв одобрен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при одобрении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/admin/reviews/{id}/reject - Отклонить отзыв
    [HttpPut("reviews/{id}/reject")]
    public async Task<IActionResult> RejectReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв отклонен и удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отклонении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/admin/dashboard - Общая статистика для админа
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetAdminDashboard()
    {
        try
        {
            var totalCars = await _context.Cars.CountAsync();
            var activeCars = await _context.Cars.CountAsync(c => c.Status == "Active");
            var totalDeals = await _context.Deals.CountAsync();
            var completedDeals = await _context.Deals.CountAsync(d => d.Status == "Completed");
            var pendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved);
            var totalUsers = await _context.AspNetUsers.CountAsync();
            var totalBrands = await _context.Brands.CountAsync();
            var totalModels = await _context.Models.CountAsync();

            return Ok(new
            {
                TotalCars = totalCars,
                ActiveCars = activeCars,
                TotalDeals = totalDeals,
                CompletedDeals = completedDeals,
                PendingReviews = pendingReviews,
                TotalUsers = totalUsers,
                TotalBrands = totalBrands,
                TotalModels = totalModels
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики админа");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/admin/users - Получить всех пользователей
    [HttpGet("users")]
    public async Task<ActionResult> GetAllUsers()
    {
        try
        {
            var users = await _context.AspNetUsers
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.City,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.IsActive
                })
                .ToListAsync();
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователей");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/admin/users/{id}/toggle-active - Активировать/деактивировать пользователя
    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(string id)
    {
        try
        {
            var user = await _context.AspNetUsers.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"Пользователь {(user.IsActive ? "активирован" : "деактивирован")}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при изменении статуса пользователя");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

