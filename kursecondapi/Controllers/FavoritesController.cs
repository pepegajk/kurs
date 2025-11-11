using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(CarPlatformContext context, ILogger<FavoritesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/favorites
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites()
    {
        try
        {
            var favorites = await _context.Favorites.ToListAsync();
            return Ok(favorites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка избранного");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/favorites/user/userId
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Favorite>>> GetFavoritesByUser(string userId)
    {
        try
        {
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .ToListAsync();
            
            return Ok(favorites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении избранного пользователя");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/favorites/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Favorite>> GetFavorite(int id)
    {
        try
        {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.Id == id);
            
            if (favorite == null)
                return NotFound(new { message = $"Избранное с ID {id} не найдено" });
            
            return Ok(favorite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении избранного");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/favorites
    [HttpPost]
    public async Task<ActionResult<Favorite>> CreateFavorite([FromBody] Favorite favorite)
    {
        try
        {
            // Проверяем, не добавлено ли уже в избранное
            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == favorite.UserId && f.CarId == favorite.CarId);

            if (existing != null)
                return BadRequest(new { message = "Эта машина уже добавлена в избранное" });

            favorite.AddedAt = DateTime.Now;
            
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetFavorite), new { id = favorite.Id }, favorite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении в избранное");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/favorites/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFavorite(int id)
    {
        try
        {
            var favorite = await _context.Favorites.FindAsync(id);
            if (favorite == null)
                return NotFound(new { message = $"Избранное с ID {id} не найдено" });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Удалено из избранного" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении из избранного");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/favorites/user/userId/car/carId
    [HttpDelete("user/{userId}/car/{carId}")]
    public async Task<IActionResult> DeleteFavoriteByUserAndCar(string userId, int carId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CarId == carId);

            if (favorite == null)
                return NotFound(new { message = "Избранное не найдено" });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Удалено из избранного" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении из избранного");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

