using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(CarPlatformContext context, ILogger<ReviewsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/reviews
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => r.IsApproved)
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка отзывов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> GetReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });
            
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/deal/5
    [HttpGet("deal/{dealId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByDeal(int dealId)
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => r.DealId == dealId && r.IsApproved)
                .Include(r => r.Author)
                .Select(r => new
                {
                    r.Id,
                    r.DealId,
                    AuthorId = r.AuthorId,
                    AuthorName = !string.IsNullOrEmpty(r.Author.FirstName) || !string.IsNullOrEmpty(r.Author.LastName)
                        ? (r.Author.FirstName + " " + r.Author.LastName).Trim()
                        : r.Author.Email ?? "Покупатель",
                    r.Rating,
                    r.Comment,
                    r.IsApproved,
                    r.CreatedAt,
                    r.UpdatedAt
                })
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отзывов по сделке");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/car/5
    [HttpGet("car/{carId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetReviewsByCar(int carId)
    {
        try
        {
            // Получаем все завершённые сделки с этим автомобилем
            var completedDealIds = await _context.Deals
                .Where(d => d.CarId == carId && d.Status == "Completed")
                .Select(d => d.Id)
                .ToListAsync();

            if (!completedDealIds.Any())
            {
                return Ok(new List<object>());
            }

            // Получаем одобренные отзывы по этим сделкам с информацией об авторе
            var reviews = await _context.Reviews
                .Where(r => completedDealIds.Contains(r.DealId) && r.IsApproved)
                .Include(r => r.Author)
                .Include(r => r.Deal)
                    .ThenInclude(d => d.Car)
                        .ThenInclude(c => c.Model)
                            .ThenInclude(m => m!.Brand)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.DealId,
                    AuthorId = r.AuthorId,
                    AuthorName = !string.IsNullOrEmpty(r.Author.FirstName) || !string.IsNullOrEmpty(r.Author.LastName)
                        ? (r.Author.FirstName + " " + r.Author.LastName).Trim()
                        : r.Author.Email ?? "Покупатель",
                    r.Rating,
                    r.Comment,
                    r.IsApproved,
                    r.CreatedAt,
                    r.UpdatedAt,
                    CarInfo = r.Deal.Car != null && r.Deal.Car.Model != null
                        ? $"{(r.Deal.Car.Model.Brand != null ? r.Deal.Car.Model.Brand.Name : "")} {r.Deal.Car.Model.Name ?? ""} {r.Deal.Car.Year}".Trim()
                        : null
                })
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отзывов по автомобилю");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/my (для пользователя - свои отзывы, включая неодобренные)
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetMyReviews()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }

            var reviews = await _context.Reviews
                .Where(r => r.AuthorId == userId)
                .Include(r => r.Author)
                .Include(r => r.Deal)
                    .ThenInclude(d => d.Car)
                        .ThenInclude(c => c.Model)
                            .ThenInclude(m => m!.Brand)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.DealId,
                    AuthorId = r.AuthorId,
                    AuthorName = !string.IsNullOrEmpty(r.Author.FirstName) || !string.IsNullOrEmpty(r.Author.LastName)
                        ? (r.Author.FirstName + " " + r.Author.LastName).Trim()
                        : r.Author.Email ?? "Покупатель",
                    r.Rating,
                    r.Comment,
                    r.IsApproved,
                    r.CreatedAt,
                    r.UpdatedAt,
                    CarInfo = r.Deal.Car != null && r.Deal.Car.Model != null
                        ? $"{(r.Deal.Car.Model.Brand != null ? r.Deal.Car.Model.Brand.Name : "")} {r.Deal.Car.Model.Name ?? ""} {r.Deal.Car.Year}".Trim()
                        : null
                })
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отзывов пользователя");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/pending (для админа - непроверенные отзывы)
    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<Review>>> GetPendingReviews()
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => !r.IsApproved)
                .Include(r => r.Author)
                .Include(r => r.Deal)
                    .ThenInclude(d => d.Car)
                        .ThenInclude(c => c.Model)
                            .ThenInclude(m => m!.Brand)
                .Select(r => new
                {
                    r.Id,
                    r.DealId,
                    AuthorId = r.AuthorId,
                    AuthorName = !string.IsNullOrEmpty(r.Author.FirstName) || !string.IsNullOrEmpty(r.Author.LastName)
                        ? (r.Author.FirstName + " " + r.Author.LastName).Trim()
                        : r.Author.Email ?? "Покупатель",
                    r.Rating,
                    r.Comment,
                    r.IsApproved,
                    r.CreatedAt,
                    r.UpdatedAt,
                    CarInfo = r.Deal.Car != null && r.Deal.Car.Model != null
                        ? $"{(r.Deal.Car.Model.Brand != null ? r.Deal.Car.Model.Brand.Name : "")} {r.Deal.Car.Model.Name ?? ""} {r.Deal.Car.Year}".Trim()
                        : null
                })
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении непроверенных отзывов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/reviews
    [HttpPost]
    [Authorize(Roles = "User,Manager,Admin")]
    public async Task<ActionResult<Review>> CreateReview([FromBody] CreateReviewRequestDto request)
    {
        try
        {
            _logger.LogInformation("=== CREATE REVIEW START ===");
            _logger.LogInformation("Request received: DealId={DealId}, Rating={Rating}, CommentLength={CommentLength}", 
                request?.DealId ?? 0, request?.Rating ?? 0, request?.Comment?.Length ?? 0);
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("UserId from token: {UserId}", userId ?? "NULL");
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }

            // Проверяем, что сделка существует и пользователь является покупателем
            var deal = await _context.Deals
                .FirstOrDefaultAsync(d => d.Id == request.DealId);

            if (deal == null)
            {
                return NotFound(new { message = $"Сделка с ID {request.DealId} не найдена" });
            }

            // Проверяем, что пользователь является покупателем в сделке
            if (deal.BuyerId != userId)
            {
                return Forbid("Вы можете оставить отзыв только по своим покупкам");
            }

            // Проверяем, что сделка завершена
            if (deal.Status != "Completed")
            {
                return BadRequest(new { message = "Отзыв можно оставить только по завершённой сделке" });
            }

            // Проверяем, что отзыв по этой сделке ещё не был оставлен
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.DealId == request.DealId && r.AuthorId == userId);

            if (existingReview != null)
            {
                return BadRequest(new { message = "Вы уже оставили отзыв по этой сделке" });
            }

            // Валидация комментария
            if (!string.IsNullOrEmpty(request.Comment) && request.Comment.Length > 1000)
            {
                return BadRequest(new { message = "Комментарий не может быть длиннее 1000 символов" });
            }

            // Создаём новый отзыв
            var review = new Review
            {
                DealId = request.DealId,
                AuthorId = userId,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment) ? string.Empty : request.Comment,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                IsApproved = false // Все отзывы требуют модерации
            };
            
            _logger.LogInformation("Review object created: DealId={DealId}, AuthorId={AuthorId}, Rating={Rating}, Comment='{Comment}', CommentLength={CommentLength}, CreatedAt={CreatedAt}", 
                review.DealId, review.AuthorId, review.Rating, review.Comment, review.Comment.Length, review.CreatedAt);
            
            try
            {
                _context.Reviews.Add(review);
                _logger.LogInformation("Review added to context. Saving changes...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Review saved successfully. ReviewId={ReviewId}", review.Id);
                
                _logger.LogInformation("Отзыв {ReviewId} создан пользователем {UserId} по сделке {DealId}", 
                    review.Id, userId, review.DealId);
                
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при создании отзыва. DealId: {DealId}, UserId: {UserId}", 
                    request.DealId, userId);
                
                // Возможно, проблема с внешними ключами
                if (dbEx.InnerException != null)
                {
                    _logger.LogError(dbEx.InnerException, "Inner exception: {Message}", dbEx.InnerException.Message);
                    return StatusCode(500, new { message = $"Ошибка базы данных: {dbEx.InnerException.Message}" });
                }
                
                return StatusCode(500, new { message = "Ошибка при сохранении отзыва в базу данных" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании отзыва. StackTrace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { message = $"Внутренняя ошибка сервера: {ex.Message}" });
        }
    }

    // POST: api/reviews/{id}/approve (модерация - одобрить отзыв)
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            review.IsApproved = true;
            review.ModeratedBy = userId;
            review.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв успешно одобрен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при одобрении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/reviews/{id}/reject (модерация - отклонить отзыв)
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RejectReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв успешно отклонен и удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отклонении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/reviews/5
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] Review review)
    {
        try
        {
            if (id != review.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingReview = await _context.Reviews.FindAsync(id);
            if (existingReview == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;
            existingReview.IsApproved = review.IsApproved;
            existingReview.ModeratedBy = review.ModeratedBy;
            existingReview.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв успешно обновлен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/reviews/5
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = $"Отзыв с ID {id} не найден" });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Отзыв успешно удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

