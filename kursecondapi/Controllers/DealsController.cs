using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using System.Security.Claims;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<DealsController> _logger;

    public DealsController(CarPlatformContext context, ILogger<DealsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/deals
    [HttpGet]
    [Authorize(Policy = "ManagerOnly")] // Используем политику, которая включает Admin, Administrator, Manager
    public async Task<ActionResult<IEnumerable<Deal>>> GetDeals()
    {
        try
        {
            // Получаем все сделки с полной информацией
            var deals = await _context.Deals
                .Include(d => d.Car)
                    .ThenInclude(c => c.Model)
                        .ThenInclude(m => m.Brand)
                .Include(d => d.Buyer)
                .Include(d => d.Seller)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            
            return Ok(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка сделок");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    // GET: api/deals/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Deal>> GetDeal(int id)
    {
        try
        {
            var deal = await _context.Deals.FirstOrDefaultAsync(d => d.Id == id);
            
            if (deal == null)
                return NotFound(new { message = $"Сделка с ID {id} не найдена" });
            
            return Ok(deal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сделки");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/deals/car/5
    [HttpGet("car/{carId}")]
    public async Task<ActionResult<IEnumerable<Deal>>> GetDealsByCar(int carId)
    {
        try
        {
            var deals = await _context.Deals
                .Where(d => d.CarId == carId)
                .ToListAsync();
            
            return Ok(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сделок по машине");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/deals/my
    // Получить все сделки текущего пользователя (как покупателя и как продавца)
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Deal>>> GetMyDeals()
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Получаем все сделки, где пользователь является покупателем или продавцом
            var deals = await _context.Deals
                .Include(d => d.Car)
                    .ThenInclude(c => c.Model)
                        .ThenInclude(m => m.Brand)
                .Include(d => d.Buyer)
                .Include(d => d.Seller)
                .Where(d => d.BuyerId == userId || d.SellerId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сделок пользователя");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    // POST: api/deals
    [HttpPost]
    [Authorize] // Разрешаем всем авторизованным пользователям
    public async Task<ActionResult<Deal>> CreateDeal([FromBody] CreateDealRequestDto dto)
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем, является ли пользователь админом/менеджером
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            
            // Создаем объект Deal из DTO (без навигационных свойств)
            var deal = new Deal
            {
                CarId = dto.CarId,
                Price = dto.Price,
                Status = dto.Status ?? (isAdmin ? "Pending" : "Created"),
                Notes = dto.Notes,
                BuyerId = string.Empty, // Будет установлено ниже
                SellerId = string.Empty // Будет установлено ниже
            };
            
            // Если пользователь не админ и не указал BuyerId - автоматически устанавливаем его
            if (!isAdmin && string.IsNullOrEmpty(dto.BuyerId))
            {
                deal.BuyerId = userId;
            }
            else if (!string.IsNullOrEmpty(dto.BuyerId))
            {
                deal.BuyerId = dto.BuyerId;
            }

            // Если BuyerId не установлен (для админов) - проверяем
            if (string.IsNullOrEmpty(deal.BuyerId))
            {
                return BadRequest(new { message = "BuyerId должен быть указан" });
            }

            // Проверяем существование машины и получаем SellerId
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.Id == dto.CarId);
            
            if (car == null)
                return NotFound(new { message = $"Машина с ID {dto.CarId} не найдена" });

            // Если SellerId не указан - берем из машины
            if (string.IsNullOrEmpty(dto.SellerId))
            {
                deal.SellerId = car.SellerId;
            }
            else
            {
                deal.SellerId = dto.SellerId;
            }

            // Проверяем, что пользователь не пытается купить свою же машину
            if (!isAdmin && deal.BuyerId == deal.SellerId)
            {
                return BadRequest(new { message = "Вы не можете купить свою собственную машину" });
            }

            // Проверяем, что машина активна (только для обычных пользователей)
            if (!isAdmin && car.Status != "Active")
            {
                return BadRequest(new { message = "Машина недоступна для покупки" });
            }

            // Проверяем, нет ли уже активной сделки (только для обычных пользователей)
            if (!isAdmin)
            {
                var existingDeal = await _context.Deals
                    .Where(d => d.CarId == dto.CarId && 
                               (d.Status == "Pending" || d.Status == "InProgress" || d.Status == "Created"))
                    .FirstOrDefaultAsync();
                
                if (existingDeal != null)
                {
                    return BadRequest(new { message = "Для этой машины уже есть активная сделка" });
                }
            }

            // Устанавливаем значения по умолчанию
            deal.CreatedAt = DateTime.Now;

            // Рассчитываем комиссию, если не указана
            if (deal.CommissionPercent == null || deal.CommissionPercent == 0)
            {
                deal.CommissionPercent = 5.0m;
            }
            if (deal.CommissionAmount == null || deal.CommissionAmount == 0)
            {
                deal.CommissionAmount = deal.Price * deal.CommissionPercent / 100;
            }
            
            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();

            // Загружаем полную информацию о сделке для ответа
            var createdDeal = await _context.Deals
                .Include(d => d.Car)
                .Include(d => d.Buyer)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(d => d.Id == deal.Id);
            
            return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, createdDeal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании сделки");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    // POST: api/deals/create
    // Создание сделки пользователем (покупателем)
    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<Deal>> CreateDealByUser([FromBody] CreateDealDto dto)
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем существование машины
            var car = await _context.Cars
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == dto.CarId);
            
            if (car == null)
                return NotFound(new { message = $"Машина с ID {dto.CarId} не найдена" });

            // Проверяем, что машина активна
            if (car.Status != "Active")
                return BadRequest(new { message = "Машина недоступна для покупки" });

            // Проверяем, что пользователь не пытается купить свою же машину
            if (car.SellerId == userId)
                return BadRequest(new { message = "Вы не можете купить свою собственную машину" });

            // Проверяем, нет ли уже активной сделки для этой машины
            var existingDeal = await _context.Deals
                .Where(d => d.CarId == dto.CarId && 
                           (d.Status == "Pending" || d.Status == "InProgress"))
                .FirstOrDefaultAsync();
            
            if (existingDeal != null)
                return BadRequest(new { message = "Для этой машины уже есть активная сделка" });

            // Создаем сделку
            var deal = new Deal
            {
                CarId = dto.CarId,
                SellerId = car.SellerId,
                BuyerId = userId,
                Price = dto.Price ?? car.Price, // Используем цену из запроса или цену машины
                Status = "Pending",
                Notes = dto.Notes,
                CreatedAt = DateTime.Now
            };

            // Рассчитываем комиссию (5% по умолчанию)
            deal.CommissionPercent = 5.0m;
            deal.CommissionAmount = deal.Price * deal.CommissionPercent / 100;

            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();

            // Загружаем полную информацию о сделке для ответа
            var createdDeal = await _context.Deals
                .Include(d => d.Car)
                .Include(d => d.Buyer)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(d => d.Id == deal.Id);

            return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, createdDeal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании сделки пользователем");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    // PUT: api/deals/5
    [HttpPut("{id}")]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> UpdateDeal(int id, [FromBody] UpdateDealDto dto)
    {
        try
        {
            // Проверка ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                
                _logger.LogWarning("Ошибки валидации при обновлении сделки {DealId}: {Errors}", id, 
                    string.Join(", ", errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors ?? new List<string>())}")));
                
                return BadRequest(new { message = "Ошибки валидации", errors });
            }

            var existingDeal = await _context.Deals.FindAsync(id);
            if (existingDeal == null)
            {
                _logger.LogWarning("Попытка обновления несуществующей сделки {DealId}", id);
                return NotFound(new { message = $"Сделка с ID {id} не найдена" });
            }

            // Обновляем только разрешенные поля (не трогаем CarId, BuyerId, SellerId, CreatedAt)
            if (!string.IsNullOrEmpty(dto.Status))
            {
                existingDeal.Status = dto.Status;
                
                // Автоматически устанавливаем CompletedAt при завершении сделки
                if (dto.Status == "Completed" && existingDeal.CompletedAt == null)
                {
                    existingDeal.CompletedAt = dto.CompletedAt ?? DateTime.Now;
                }
                // Сбрасываем CompletedAt, если сделка не завершена
                else if (dto.Status != "Completed" && existingDeal.CompletedAt != null)
                {
                    existingDeal.CompletedAt = null;
                }
            }

            // Обновляем CompletedAt, если он явно указан в DTO
            if (dto.CompletedAt.HasValue)
            {
                existingDeal.CompletedAt = dto.CompletedAt.Value;
            }

            if (dto.Price.HasValue && dto.Price.Value > 0)
            {
                existingDeal.Price = dto.Price.Value;
            }

            if (dto.CommissionAmount.HasValue)
            {
                existingDeal.CommissionAmount = dto.CommissionAmount.Value;
            }

            if (dto.CommissionPercent.HasValue)
            {
                existingDeal.CommissionPercent = dto.CommissionPercent.Value;
            }

            if (dto.Notes != null)
            {
                existingDeal.Notes = dto.Notes;
            }

            // Устанавливаем ApprovedBy для админов/менеджеров
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            if (isAdmin && !string.IsNullOrEmpty(userId))
            {
                existingDeal.ApprovedBy = userId;
            }
            else if (dto.ApprovedBy != null)
            {
                existingDeal.ApprovedBy = dto.ApprovedBy;
            }

            // Пересчитываем комиссию, если изменилась цена или процент
            if (dto.Price.HasValue || dto.CommissionPercent.HasValue)
            {
                if (existingDeal.CommissionPercent.HasValue && existingDeal.CommissionPercent.Value > 0)
                {
                    existingDeal.CommissionAmount = existingDeal.Price * existingDeal.CommissionPercent.Value / 100;
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Сделка {DealId} успешно обновлена пользователем {UserId}", id, userId);
            
            // Возвращаем обновленную сделку с полной информацией
            var updatedDeal = await _context.Deals
                .Include(d => d.Car)
                    .ThenInclude(c => c.Model)
                        .ThenInclude(m => m.Brand)
                .Include(d => d.Buyer)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(d => d.Id == id);

            return Ok(updatedDeal);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Ошибка конкурентного доступа при обновлении сделки {DealId}", id);
            return StatusCode(409, new { message = "Сделка была изменена другим пользователем. Обновите страницу и попробуйте снова." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Ошибка базы данных при обновлении сделки {DealId}", id);
            return StatusCode(500, new { message = "Ошибка при сохранении в базу данных", details = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении сделки {DealId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // DELETE: api/deals/5
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteDeal(int id)
    {
        try
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null)
                return NotFound(new { message = $"Сделка с ID {id} не найдена" });

            _context.Deals.Remove(deal);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Сделка успешно удалена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении сделки");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

// DTO для создания сделки пользователем
public class CreateDealDto
{
    public int CarId { get; set; }
    public decimal? Price { get; set; } // Опционально, если не указано - берется цена машины
    public string? Notes { get; set; }
}

// DTO для создания сделки через POST /api/deals (без навигационных свойств)
public class CreateDealRequestDto
{
    public int CarId { get; set; }
    public decimal Price { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? BuyerId { get; set; }  // Опционально, API установит автоматически
    public string? SellerId { get; set; } // Опционально, API установит из машины
}

// DTO для обновления сделки через PUT /api/deals/{id}
public class UpdateDealDto
{
    public string? Status { get; set; }
    public decimal? Price { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? CommissionPercent { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ApprovedBy { get; set; }
}
