using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

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
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetMainImageByCar(int carId)
    {
        try
        {
            // Проверяем существование автомобиля
            var carExists = await _context.Cars.AnyAsync(c => c.Id == carId);
            if (!carExists)
            {
                return NotFound(new { message = $"Автомобиль с ID {carId} не найден" });
            }

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

            // Если изображений нет, возвращаем дефолтное изображение
            if (mainImage == null)
            {
                return Ok(new
                {
                    Id = 0,
                    CarId = carId,
                    ImageUrl = "/images/cars/default-car.jpg", // Дефолтное изображение
                    Title = "Изображение отсутствует",
                    DisplayOrder = 0,
                    IsMain = true,
                    UploadedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });
            }
            
            return Ok(mainImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении главного изображения для автомобиля {CarId}", carId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    // POST: api/carimages
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCarImage(int id)
    {
        try
        {
            var image = await _context.CarImages.FindAsync(id);
            if (image == null)
                return NotFound(new { message = $"Изображение с ID {id} не найдено" });

            var carId = image.CarId;
            var wasMain = image.IsMain;
            var imageUrl = image.ImageUrl;

            // Удаляем физический файл с диска
            if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/images/"))
            {
                try
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Физический файл удален: {FilePath}", filePath);
                    }
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning(fileEx, "Не удалось удалить физический файл: {ImageUrl}", imageUrl);
                    // Продолжаем удаление из БД даже если файл не удалился
                }
            }

            // Удаляем запись из базы данных
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
            
            return Ok(new { message = "Изображение успешно удалено", deletedImageId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении изображения");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
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

    // POST: api/carimages/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CarImage>> UploadCarImageFile([FromForm] DTOs.UploadCarImageDto dto)
    {
        try
        {
            // 1. Валидация файла
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "Файл не был загружен или пуст" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                return BadRequest(new { message = "Недопустимый тип файла. Разрешенные форматы: .jpg, .jpeg, .png, .gif, .webp" });

            const long maxFileSize = 10 * 1024 * 1024; // 10 МБ
            if (dto.File.Length > maxFileSize)
                return BadRequest(new { message = "Размер файла превышает 10 МБ" });

            // 2. Проверка существования машины
            var carExists = await _context.Cars.AnyAsync(c => c.Id == dto.CarId);
            if (!carExists)
                return NotFound(new { message = $"Машина с ID {dto.CarId} не найдена" });

            // 3. Создание уникального имени файла
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            // 4. Сохранение файла в wwwroot/images/cars/
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "cars");
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // 5. Создание объекта CarImage
            var carImage = new CarImage
            {
                CarId = dto.CarId,
                ImageUrl = $"/images/cars/{fileName}",
                Title = dto.File.FileName,
                UploadedAt = DateTime.Now
            };

            // 6. Определение IsMain и DisplayOrder
            var existingImages = await _context.CarImages
                .Where(ci => ci.CarId == dto.CarId)
                .ToListAsync();

            if (existingImages.Count == 0)
            {
                // Первое изображение для машины - делаем его главным
                carImage.IsMain = true;
                carImage.DisplayOrder = 0;
            }
            else
            {
                // Не первое изображение - устанавливаем следующий порядок
                var maxOrder = existingImages.Max(ci => ci.DisplayOrder);
                carImage.DisplayOrder = maxOrder + 1;
                carImage.IsMain = false;
            }

            // Если новое изображение отмечено как главное (через параметр), убираем флаг у остальных
            // Но по умолчанию делаем главным только первое изображение
            // Если нужно сделать главным - можно добавить параметр bool isMain = false

            // 7. Сохранение в базу данных
            _context.CarImages.Add(carImage);
            await _context.SaveChangesAsync();

            // 8. Возврат результата
            return CreatedAtAction(nameof(GetCarImage), new { id = carImage.Id }, carImage);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Ошибка доступа при сохранении файла");
            return StatusCode(500, new { message = "Ошибка доступа к файловой системе" });
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория не найдена");
            return StatusCode(500, new { message = "Ошибка создания директории для файлов" });
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Ошибка ввода-вывода при сохранении файла");
            return StatusCode(500, new { message = "Ошибка при сохранении файла на диск" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке изображения");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера при загрузке файла" });
        }
    }
}

