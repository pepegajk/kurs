using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using kursecondapi.DTOs;
using System.Security.Claims;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<CarsController> _logger;

    public CarsController(CarPlatformContext context, ILogger<CarsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/cars (Доступно всем)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetCars()
    {
        try
        {
            var cars = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Select(c => new
                {
                    c.Id,
                    c.ModelId,
                    c.SellerId,
                    c.Year,
                    c.Price,
                    c.Mileage,
                    c.Color,
                    c.BodyType,
                    c.FuelType,
                    c.Transmission,
                    c.DriveType,
                    c.EngineVolume,
                    c.EnginePower,
                    c.Vin,
                    c.RegistrationNumber,
                    c.Description,
                    c.Location,
                    c.Condition,
                    c.ViewsCount,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.Status,
                    c.IsFeatured,
                    BrandName = c.Model.Brand.Name,
                    ModelName = c.Model.Name
                })
                .ToListAsync();
            return Ok(cars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка машин");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/cars/5 (Доступно всем)
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetCar(int id)
    {
        try
        {
            var car = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });
            
            // Возвращаем объект с нужными полями, избегая проблем с сериализацией навигационных свойств
            var result = new
            {
                car.Id,
                car.ModelId,
                car.SellerId,
                SellerName = car.Seller != null ? $"{car.Seller.FirstName} {car.Seller.LastName}" : null,
                SellerEmail = car.Seller?.Email,
                car.Year,
                car.Price,
                car.Mileage,
                car.Color,
                car.BodyType,
                car.FuelType,
                car.Transmission,
                car.DriveType,
                car.EngineVolume,
                car.EnginePower,
                car.Vin,
                car.RegistrationNumber,
                car.Description,
                car.Location,
                car.Condition,
                car.ViewsCount,
                car.CreatedAt,
                car.UpdatedAt,
                car.Status,
                car.IsFeatured,
                BrandName = car.Model?.Brand?.Name,
                ModelName = car.Model?.Name
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении машины с ID {CarId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // POST: api/cars
    [HttpPost]
    [Authorize(Roles = "Dealer,Manager,Admin,Administrator")]
    public async Task<ActionResult<Car>> CreateCar([FromBody] CreateCarDto dto)
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем, является ли пользователь админом/менеджером
            var isAdminOrManager = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            
            // Создаем объект Car из DTO
            var car = new Car
            {
                ModelId = dto.ModelId,
                SellerId = userId, // Автоматически устанавливаем SellerId из токена
                Year = dto.Year,
                Price = dto.Price,
                Mileage = dto.Mileage,
                Color = dto.Color,
                BodyType = dto.BodyType,
                FuelType = dto.FuelType,
                Transmission = dto.Transmission,
                DriveType = dto.DriveType,
                EngineVolume = dto.EngineVolume,
                EnginePower = dto.EnginePower,
                Vin = dto.Vin,
                RegistrationNumber = dto.RegistrationNumber,
                Description = dto.Description,
                Location = dto.Location,
                Condition = dto.Condition,
                IsFeatured = dto.IsFeatured,
                ViewsCount = 0,
                Status = isAdminOrManager ? "Active" : "Pending", // Для дилера - на модерации
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };
            
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            
            // Загружаем полную информацию о машине для ответа
            var createdCar = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == car.Id);
            
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, createdCar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании машины");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // PUT: api/cars/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Dealer,Manager,Admin,Administrator")]
    public async Task<IActionResult> UpdateCar(int id, [FromBody] UpdateCarDto dto)
    {
        try
        {
            // Проверка валидации модели
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = x.Value?.Errors.Select(e => e.ErrorMessage)
                    })
                    .ToList();
                
                _logger.LogWarning("Ошибки валидации при обновлении машины {CarId}: {Errors}", id, string.Join("; ", errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors ?? Array.Empty<string>())}")));
                
                return BadRequest(new
                {
                    message = "Ошибки валидации",
                    errors = errors
                });
            }

            // Логирование входных данных для диагностики
            _logger.LogInformation("Обновление машины {CarId}. Изменяемые поля: {Fields}", id, 
                string.Join(", ", GetChangedFields(dto)));

            var existingCar = await _context.Cars.FindAsync(id);
            if (existingCar == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем права доступа: дилер может обновлять только свои машины
            var isAdminOrManager = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!isAdminOrManager && existingCar.SellerId != userId)
            {
                return Forbid("Вы можете обновлять только свои автомобили");
            }

            // Обновляем только указанные поля
            if (dto.ModelId.HasValue)
            {
                // Проверяем существование модели
                var modelExists = await _context.Models.AnyAsync(m => m.Id == dto.ModelId.Value);
                if (!modelExists)
                {
                    _logger.LogWarning("Попытка обновить машину {CarId} с несуществующей моделью {ModelId}", id, dto.ModelId.Value);
                    return BadRequest(new { message = $"Модель с ID {dto.ModelId.Value} не найдена" });
                }
                existingCar.ModelId = dto.ModelId.Value;
            }
            if (dto.Year.HasValue)
                existingCar.Year = dto.Year.Value;
            if (dto.Price.HasValue)
                existingCar.Price = dto.Price.Value;
            if (dto.Mileage.HasValue)
                existingCar.Mileage = dto.Mileage.Value;
            if (!string.IsNullOrEmpty(dto.Color))
                existingCar.Color = dto.Color;
            if (!string.IsNullOrEmpty(dto.BodyType))
                existingCar.BodyType = dto.BodyType;
            if (!string.IsNullOrEmpty(dto.FuelType))
                existingCar.FuelType = dto.FuelType;
            if (!string.IsNullOrEmpty(dto.Transmission))
                existingCar.Transmission = dto.Transmission;
            if (!string.IsNullOrEmpty(dto.DriveType))
                existingCar.DriveType = dto.DriveType;
            if (dto.EngineVolume.HasValue)
                existingCar.EngineVolume = dto.EngineVolume;
            if (dto.EnginePower.HasValue)
                existingCar.EnginePower = dto.EnginePower;
            if (dto.Vin != null)
            {
                // Проверяем уникальность VIN - он не должен использоваться другим автомобилем
                var vinExists = await _context.Cars
                    .AnyAsync(c => c.Vin == dto.Vin && c.Id != id);
                if (vinExists)
                    return BadRequest(new { message = $"VIN '{dto.Vin}' уже используется другим автомобилем" });
                
                existingCar.Vin = dto.Vin;
            }
            if (dto.RegistrationNumber != null)
                existingCar.RegistrationNumber = dto.RegistrationNumber;
            if (dto.Description != null)
                existingCar.Description = dto.Description;
            if (dto.Location != null)
                existingCar.Location = dto.Location;
            if (dto.Condition != null)
                existingCar.Condition = dto.Condition;
            if (dto.Status != null && isAdminOrManager) // Только менеджер/админ может менять статус
                existingCar.Status = dto.Status;
            if (dto.IsFeatured.HasValue && isAdminOrManager) // Только менеджер/админ может менять IsFeatured
                existingCar.IsFeatured = dto.IsFeatured.Value;

            existingCar.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();
            
            // Загружаем обновленную машину с полной информацией
            var updatedCar = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            return Ok(updatedCar);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Обработка ошибки уникального ключа (дубликат VIN или другого уникального поля)
            var constraintName = pgEx.ConstraintName;
            string errorMessage = constraintName switch
            {
                "Cars_VIN_key" => "VIN номер уже используется другим автомобилем",
                _ => "Нарушение уникальности данных. Проверьте, что все уникальные поля (VIN и т.д.) не дублируются."
            };
            
            _logger.LogWarning(ex, "Нарушение уникальности при обновлении машины с ID {CarId}: {ConstraintName}", id, constraintName);
            return Conflict(new { message = errorMessage, constraint = constraintName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении машины с ID {CarId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // DELETE: api/cars/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Dealer,Manager,Admin,Administrator")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        try
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем права доступа: дилер может удалять только свои машины
            var isAdminOrManager = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!isAdminOrManager && car.SellerId != userId)
            {
                return Forbid("Вы можете удалять только свои автомобили");
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Машина успешно удалена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении машины");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // GET: api/cars/active
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<VwActiveCar>>> GetActiveCars()
    {
        try
        {
            var activeCars = await _context.VwActiveCars.ToListAsync();
            return Ok(activeCars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении активных машин");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/cars/seller/{sellerId}
    [HttpGet("seller/{sellerId}")]
    [Authorize(Roles = "Dealer,Manager,Admin,Administrator")]
    public async Task<ActionResult<IEnumerable<object>>> GetCarsBySeller(string sellerId)
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            // Проверяем права доступа: дилер может видеть только свои машины
            var isAdminOrManager = User.IsInRole("Administrator") || User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!isAdminOrManager && sellerId != userId)
            {
                return Forbid("Вы можете просматривать только свои автомобили");
            }

            var cars = await _context.Cars
                .Where(c => c.SellerId == sellerId)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .Select(c => new
                {
                    c.Id,
                    c.ModelId,
                    c.SellerId,
                    SellerName = c.Seller.FirstName + " " + c.Seller.LastName,
                    c.Year,
                    c.Price,
                    c.Mileage,
                    c.Color,
                    c.BodyType,
                    c.FuelType,
                    c.Transmission,
                    c.DriveType,
                    c.EngineVolume,
                    c.EnginePower,
                    c.Vin,
                    c.RegistrationNumber,
                    c.Description,
                    c.Location,
                    c.Condition,
                    c.ViewsCount,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.Status,
                    c.IsFeatured,
                    BrandName = c.Model.Brand.Name,
                    ModelName = c.Model.Name
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(cars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении машин продавца");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // GET: api/cars/pending
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin,Administrator")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingCars()
    {
        try
        {
            var pendingCars = await _context.Cars
                .Where(c => c.Status == "Pending" || c.Status == "Inactive")
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .Select(c => new
                {
                    c.Id,
                    c.ModelId,
                    c.SellerId,
                    SellerName = c.Seller.FirstName + " " + c.Seller.LastName,
                    SellerEmail = c.Seller.Email,
                    c.Year,
                    c.Price,
                    c.Mileage,
                    c.Color,
                    c.BodyType,
                    c.FuelType,
                    c.Transmission,
                    c.DriveType,
                    c.EngineVolume,
                    c.EnginePower,
                    c.Vin,
                    c.RegistrationNumber,
                    c.Description,
                    c.Location,
                    c.Condition,
                    c.ViewsCount,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.Status,
                    c.IsFeatured,
                    BrandName = c.Model.Brand.Name,
                    ModelName = c.Model.Name
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(pendingCars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении машин на модерации");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // POST: api/cars/{id}/approve
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Manager,Admin,Administrator")]
    public async Task<IActionResult> ApproveCar(int id)
    {
        try
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            car.Status = "Active";
            car.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();

            // Загружаем обновленную машину с полной информацией
            var updatedCar = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id);

            return Ok(new { message = "Автомобиль успешно одобрен", car = updatedCar });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при одобрении автомобиля");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // POST: api/cars/{id}/reject
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Manager,Admin,Administrator")]
    public async Task<IActionResult> RejectCar(int id, [FromBody] RejectCarDto? dto = null)
    {
        try
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            car.Status = "Rejected";
            car.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();

            // Загружаем обновленную машину с полной информацией
            var updatedCar = await _context.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id);

            return Ok(new { message = "Автомобиль отклонен", rejectionReason = dto?.RejectionReason, car = updatedCar });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отклонении автомобиля");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // Вспомогательный метод для логирования изменяемых полей
    private static List<string> GetChangedFields(UpdateCarDto dto)
    {
        var fields = new List<string>();
        
        if (dto.ModelId.HasValue) fields.Add($"ModelId={dto.ModelId.Value}");
        if (dto.Year.HasValue) fields.Add($"Year={dto.Year.Value}");
        if (dto.Price.HasValue) fields.Add($"Price={dto.Price.Value}");
        if (dto.Mileage.HasValue) fields.Add($"Mileage={dto.Mileage.Value}");
        if (!string.IsNullOrEmpty(dto.Color)) fields.Add("Color");
        if (!string.IsNullOrEmpty(dto.BodyType)) fields.Add("BodyType");
        if (!string.IsNullOrEmpty(dto.FuelType)) fields.Add("FuelType");
        if (!string.IsNullOrEmpty(dto.Transmission)) fields.Add("Transmission");
        if (!string.IsNullOrEmpty(dto.DriveType)) fields.Add("DriveType");
        if (dto.EngineVolume.HasValue) fields.Add($"EngineVolume={dto.EngineVolume.Value}");
        if (dto.EnginePower.HasValue) fields.Add($"EnginePower={dto.EnginePower.Value}");
        if (dto.Vin != null) fields.Add("Vin");
        if (dto.RegistrationNumber != null) fields.Add("RegistrationNumber");
        if (dto.Description != null) fields.Add("Description");
        if (dto.Location != null) fields.Add("Location");
        if (dto.Condition != null) fields.Add("Condition");
        if (dto.Status != null) fields.Add($"Status={dto.Status}");
        if (dto.IsFeatured.HasValue) fields.Add($"IsFeatured={dto.IsFeatured.Value}");
        
        return fields;
    }
}

// DTO для отклонения автомобиля
public class RejectCarDto
{
    public string? RejectionReason { get; set; }
}

