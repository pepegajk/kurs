using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(CarPlatformContext context, ILogger<ModelsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/models
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Model>>> GetModels()
    {
        try
        {
            var models = await _context.Models
                .Where(m => m.IsActive)
                .ToListAsync();
            
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка моделей");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/models/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Model>> GetModel(int id)
    {
        try
        {
            var model = await _context.Models.FirstOrDefaultAsync(m => m.Id == id);
            
            if (model == null)
                return NotFound(new { message = $"Модель с ID {id} не найдена" });
            
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении модели");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/models/brand/5
    [HttpGet("brand/{brandId}")]
    public async Task<ActionResult<IEnumerable<Model>>> GetModelsByBrand(int brandId)
    {
        try
        {
            var models = await _context.Models
                .Where(m => m.BrandId == brandId && m.IsActive)
                .ToListAsync();
            
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении моделей по бренду");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/models
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Model>> CreateModel([FromBody] Model model)
    {
        try
        {
            model.CreatedAt = DateTime.Now;
            model.IsActive = true;
            
            _context.Models.Add(model);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetModel), new { id = model.Id }, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании модели");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/models/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateModel(int id, [FromBody] Model model)
    {
        try
        {
            if (id != model.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingModel = await _context.Models.FindAsync(id);
            if (existingModel == null)
                return NotFound(new { message = $"Модель с ID {id} не найдена" });

            existingModel.BrandId = model.BrandId;
            existingModel.Name = model.Name;
            existingModel.Description = model.Description;
            existingModel.YearFrom = model.YearFrom;
            existingModel.YearTo = model.YearTo;
            existingModel.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Модель успешно обновлена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении модели");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/models/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteModel(int id)
    {
        try
        {
            var model = await _context.Models.FindAsync(id);
            if (model == null)
                return NotFound(new { message = $"Модель с ID {id} не найдена" });

            _context.Models.Remove(model);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Модель успешно удалена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении модели");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

