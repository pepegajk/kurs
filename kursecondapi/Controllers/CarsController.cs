using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

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
    public async Task<ActionResult<IEnumerable<Car>>> GetCars()
    {
        try
        {
            var cars = await _context.Cars.ToListAsync();
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
    public async Task<ActionResult<Car>> GetCar(int id)
    {
        try
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id);
            
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });
            
            return Ok(car);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/cars
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Car>> CreateCar([FromBody] Car car)
    {
        try
        {
            car.CreatedAt = DateTime.Now;
            car.UpdatedAt = DateTime.Now;
            car.ViewsCount = 0;
            
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/cars/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCar(int id, [FromBody] Car car)
    {
        try
        {
            if (id != car.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingCar = await _context.Cars.FindAsync(id);
            if (existingCar == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            existingCar.ModelId = car.ModelId;
            existingCar.Year = car.Year;
            existingCar.Price = car.Price;
            existingCar.Mileage = car.Mileage;
            existingCar.Color = car.Color;
            existingCar.BodyType = car.BodyType;
            existingCar.FuelType = car.FuelType;
            existingCar.Transmission = car.Transmission;
            existingCar.DriveType = car.DriveType;
            existingCar.EngineVolume = car.EngineVolume;
            existingCar.EnginePower = car.EnginePower;
            existingCar.Description = car.Description;
            existingCar.Location = car.Location;
            existingCar.Condition = car.Condition;
            existingCar.Status = car.Status;
            existingCar.IsFeatured = car.IsFeatured;
            existingCar.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Машина успешно обновлена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/cars/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        try
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound(new { message = $"Машина с ID {id} не найдена" });

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Машина успешно удалена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
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
}

