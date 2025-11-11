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
                .ToListAsync();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отзывов по сделке");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/reviews/pending (для админа - непроверенные отзывы)
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Review>>> GetPendingReviews()
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => !r.IsApproved)
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
    public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
    {
        try
        {
            review.CreatedAt = DateTime.Now;
            review.IsApproved = false;
            
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании отзыва");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/reviews/{id}/approve (модерация - одобрить отзыв)
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
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
            review.UpdatedAt = DateTime.Now;

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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
            existingReview.UpdatedAt = DateTime.Now;

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
    [Authorize(Roles = "Admin")]
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

