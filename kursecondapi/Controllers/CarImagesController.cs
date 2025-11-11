using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarImagesController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly ILogger<CarImagesController> _logger;

    public CarImagesController(CarPlatformContext context, ILogger<CarImagesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/carimages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarImage>>> GetCarImages()
    {
        try
        {
            var images = await _context.CarImages
                .OrderBy(ci => ci.CarId)
                    .ThenBy(ci => ci.DisplayOrder)
                .ToListAsync();
            
            return Ok(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка изображений");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/carimages/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CarImage>> GetCarImage(int id)
    {
        try
        {
            var image = await _context.CarImages.FirstOrDefaultAsync(ci => ci.Id == id);
            
            if (image == null)
                return NotFound(new { message = $"Изображение с ID {id} не найдено" });
            
            return Ok(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/carimages/car/5
    [HttpGet("car/{carId}")]
    public async Task<ActionResult<IEnumerable<CarImage>>> GetImagesByCar(int carId)
    {
        try
        {
            var images = await _context.CarImages
                .Where(ci => ci.CarId == carId)
                .OrderBy(ci => ci.DisplayOrder)
                .ToListAsync();
            
            return Ok(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении изображений машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/carimages/car/5/main
    [HttpGet("car/{carId}/main")]
    public async Task<ActionResult<CarImage>> GetMainImageByCar(int carId)
    {
        try
        {
            var mainImage = await _context.CarImages
                .Where(ci => ci.CarId == carId && ci.IsMain)
                .FirstOrDefaultAsync();
            
            if (mainImage == null)
            {
                // Если главного изображения нет, вернуть первое по порядку
                mainImage = await _context.CarImages
                    .Where(ci => ci.CarId == carId)
                    .OrderBy(ci => ci.DisplayOrder)
                    .FirstOrDefaultAsync();
            }

            if (mainImage == null)
                return NotFound(new { message = $"Изображения для машины с ID {carId} не найдены" });
            
            return Ok(mainImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении главного изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/carimages
    [HttpPost]
    public async Task<ActionResult<CarImage>> CreateCarImage([FromBody] CarImage carImage)
    {
        try
        {
            // Проверяем существование машины
            var carExists = await _context.Cars.AnyAsync(c => c.Id == carImage.CarId);
            if (!carExists)
                return BadRequest(new { message = $"Машина с ID {carImage.CarId} не найдена" });

            carImage.UploadedAt = DateTime.Now;
            
            // Если это первое изображение для машины, делаем его главным
            var existingImages = await _context.CarImages
                .Where(ci => ci.CarId == carImage.CarId)
                .CountAsync();
            
            if (existingImages == 0)
            {
                carImage.IsMain = true;
                carImage.DisplayOrder = 0;
            }
            else if (carImage.DisplayOrder == 0)
            {
                // Автоматически ставим следующий порядок
                var maxOrder = await _context.CarImages
                    .Where(ci => ci.CarId == carImage.CarId)
                    .MaxAsync(ci => ci.DisplayOrder);
                carImage.DisplayOrder = maxOrder + 1;
            }

            // Если новое изображение отмечено как главное, убираем флаг у остальных
            if (carImage.IsMain)
            {
                var otherImages = await _context.CarImages
                    .Where(ci => ci.CarId == carImage.CarId && ci.IsMain)
                    .ToListAsync();
                
                foreach (var img in otherImages)
                {
                    img.IsMain = false;
                }
            }
            
            _context.CarImages.Add(carImage);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetCarImage), new { id = carImage.Id }, carImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/carimages/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCarImage(int id, [FromBody] CarImage carImage)
    {
        try
        {
            if (id != carImage.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingImage = await _context.CarImages.FindAsync(id);
            if (existingImage == null)
                return NotFound(new { message = $"Изображение с ID {id} не найдено" });

            // Если изображение отмечается как главное, убираем флаг у остальных
            if (carImage.IsMain && !existingImage.IsMain)
            {
                var otherImages = await _context.CarImages
                    .Where(ci => ci.CarId == carImage.CarId && ci.IsMain && ci.Id != id)
                    .ToListAsync();
                
                foreach (var img in otherImages)
                {
                    img.IsMain = false;
                }
            }

            existingImage.ImageUrl = carImage.ImageUrl;
            existingImage.Title = carImage.Title;
            existingImage.DisplayOrder = carImage.DisplayOrder;
            existingImage.IsMain = carImage.IsMain;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Изображение успешно обновлено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/carimages/5/setmain
    [HttpPut("{id}/setmain")]
    public async Task<IActionResult> SetMainImage(int id)
    {
        try
        {
            var image = await _context.CarImages.FindAsync(id);
            if (image == null)
                return NotFound(new { message = $"Изображение с ID {id} не найдено" });

            // Убираем флаг главного изображения у других
            var otherImages = await _context.CarImages
                .Where(ci => ci.CarId == image.CarId && ci.IsMain && ci.Id != id)
                .ToListAsync();
            
            foreach (var img in otherImages)
            {
                img.IsMain = false;
            }

            image.IsMain = true;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Изображение установлено как главное" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при установке главного изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/carimages/car/5/reorder
    [HttpPut("car/{carId}/reorder")]
    public async Task<IActionResult> ReorderImages(int carId, [FromBody] List<int> imageIds)
    {
        try
        {
            var images = await _context.CarImages
                .Where(ci => ci.CarId == carId)
                .ToListAsync();

            for (int i = 0; i < imageIds.Count; i++)
            {
                var image = images.FirstOrDefault(img => img.Id == imageIds[i]);
                if (image != null)
                {
                    image.DisplayOrder = i;
                }
            }

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Порядок изображений обновлен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при изменении порядка изображений");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/carimages/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarImage(int id)
    {
        try
        {
            var image = await _context.CarImages.FindAsync(id);
            if (image == null)
                return NotFound(new { message = $"Изображение с ID {id} не найдено" });

            var carId = image.CarId;
            var wasMain = image.IsMain;

            _context.CarImages.Remove(image);
            await _context.SaveChangesAsync();

            // Если удалили главное изображение, назначаем первое по порядку как главное
            if (wasMain)
            {
                var newMainImage = await _context.CarImages
                    .Where(ci => ci.CarId == carId)
                    .OrderBy(ci => ci.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (newMainImage != null)
                {
                    newMainImage.IsMain = true;
                    await _context.SaveChangesAsync();
                }
            }
            
            return Ok(new { message = "Изображение успешно удалено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении изображения");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/carimages/car/5
    [HttpDelete("car/{carId}")]
    public async Task<IActionResult> DeleteAllCarImages(int carId)
    {
        try
        {
            var images = await _context.CarImages
                .Where(ci => ci.CarId == carId)
                .ToListAsync();

            if (!images.Any())
                return NotFound(new { message = $"Изображения для машины с ID {carId} не найдены" });

            _context.CarImages.RemoveRange(images);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"Удалено изображений: {images.Count}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении всех изображений машины");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}

