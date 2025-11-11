using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IEnumerable<Deal>>> GetDeals()
    {
        try
        {
            var deals = await _context.Deals.ToListAsync();
            return Ok(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка сделок");
            return StatusCode(500, "Внутренняя ошибка сервера");
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

    // POST: api/deals
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Deal>> CreateDeal([FromBody] Deal deal)
    {
        try
        {
            deal.CreatedAt = DateTime.Now;
            deal.Status = "Pending";
            
            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, deal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании сделки");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/deals/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDeal(int id, [FromBody] Deal deal)
    {
        try
        {
            if (id != deal.Id)
                return BadRequest(new { message = "ID не совпадает" });

            var existingDeal = await _context.Deals.FindAsync(id);
            if (existingDeal == null)
                return NotFound(new { message = $"Сделка с ID {id} не найдена" });

            existingDeal.Status = deal.Status;
            existingDeal.Price = deal.Price;
            existingDeal.CommissionAmount = deal.CommissionAmount;
            existingDeal.CommissionPercent = deal.CommissionPercent;
            existingDeal.Notes = deal.Notes;
            existingDeal.ApprovedBy = deal.ApprovedBy;
            
            if (deal.Status == "Completed" && existingDeal.CompletedAt == null)
                existingDeal.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Сделка успешно обновлена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении сделки");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // DELETE: api/deals/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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

