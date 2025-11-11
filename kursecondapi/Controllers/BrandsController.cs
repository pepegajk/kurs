using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(CarPlatformContext context, ILogger<BrandsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/brands
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
    {
        try
        {
            var brands = await _context.Brands
                .Where(b => b.IsActive)
                .ToListAsync();
            
            return Ok(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка брендов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/brands/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Brand>> GetBrand(int id)
    {
        try
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
            
            if (brand == null)
                return NotFound(new { message = $"Бренд с ID {id} не найден" });
            
            return Ok(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении бренда");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/brands
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Brand>> CreateBrand([FromBody] Brand brand)
    {
        try
        {
            brand.CreatedAt = DateTime.Now;
            brand.IsActive = true;
            
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании бренда");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/brands/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] Brand brand)
    {
        try
        {
            if (id != brand.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingBrand = await _context.Brands.FindAsync(id);
            if (existingBrand == null)
                return NotFound(new { message = $"Бренд с ID {id} не найден" });

            existingBrand.Name = brand.Name;
            existingBrand.Description = brand.Description;
            existingBrand.LogoUrl = brand.LogoUrl;
            existingBrand.Country = brand.Country;
            existingBrand.IsActive = brand.IsActive;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Бренд успешно обновлен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении бренда");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/brands/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        try
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound(new { message = $"Бренд с ID {id} не найден" });

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Бренд успешно удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении бренда");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

