using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using kursecondapi.Models;
using kursecondapi.DTOs;
using kursecondapi.Services;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CarPlatformContext _context;
    private readonly PasswordHasherService _passwordHasher;
    private readonly UserManager<AspNetUser> _userManager;
    private readonly RoleManager<AspNetRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        CarPlatformContext context,
        PasswordHasherService passwordHasher,
        UserManager<AspNetUser> userManager,
        RoleManager<AspNetRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
    {
        try
        {
            // Проверяем существование пользователя
            var userExists = await _context.AspNetUsers
                .AnyAsync(u => u.Email == model.Email || u.NormalizedEmail == model.Email.ToUpperInvariant());
            
            if (userExists)
                return BadRequest(new { message = "Пользователь с таким email уже существует" });

            // Хешируем пароль используя SHA-256
            var hashedPassword = _passwordHasher.HashPassword(model.Password);

            // Создаем нового пользователя
            var user = new AspNetUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpperInvariant(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpperInvariant(),
                PasswordHash = hashedPassword, // Сохраняем SHA-256 хеш
                SecurityStamp = Guid.NewGuid().ToString(), // Обязательное поле для Identity
                ConcurrencyStamp = Guid.NewGuid().ToString(), // Обязательное поле для Identity
                FirstName = model.FirstName ?? string.Empty, // Если null, ставим пустую строку
                LastName = model.LastName ?? string.Empty, // Если null, ставим пустую строку
                PhoneNumber = model.PhoneNumber,
                City = model.City,
                Address = model.Address,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                LockoutEnd = null,
                TwoFactorEnabled = false,
                AccessFailedCount = 0
            };

            // Сохраняем пользователя в БД
            _context.AspNetUsers.Add(user);
            await _context.SaveChangesAsync();

            // Назначаем роль User по умолчанию
            var userRole = await _context.AspNetRoles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole != null)
            {
                // Добавляем связь пользователь-роль в промежуточную таблицу
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"AspNetUserRoles\" (\"UserId\", \"RoleId\") VALUES ({0}, {1})",
                    user.Id, userRole.Id);
                await _context.SaveChangesAsync();
            }

            var token = await GenerateJwtToken(user);
            var roles = await GetUserRolesAsync(user.Id);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                ExpiresAt = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"]))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
    {
        try
        {
            // Находим пользователя по email
            var user = await _context.AspNetUsers
                .FirstOrDefaultAsync(u => u.Email == model.Email || u.NormalizedEmail == model.Email.ToUpperInvariant());
            
            if (user == null)
                return Unauthorized(new { message = "Неверный email или пароль" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Ваш аккаунт заблокирован" });

            // Проверяем пароль используя SHA-256 хеширование
            if (string.IsNullOrEmpty(user.PasswordHash) || 
                !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            // Обновляем LastLoginAt
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var token = await GenerateJwtToken(user);
            var roles = await GetUserRolesAsync(user.Id);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                ExpiresAt = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"]))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // POST: api/auth/assign-role (Admin only)
    [HttpPost("assign-role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return BadRequest(new { message = $"Роль '{model.RoleName}' не существует" });

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Ошибка при назначении роли: {errors}" });
            }

            return Ok(new { message = $"Роль '{model.RoleName}' успешно назначена пользователю {user.Email}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при назначении роли");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // PUT: api/auth/change-role (Admin only)
    // Изменяет роль пользователя: удаляет все старые роли и назначает новую через таблицу AspNetUserRoles
    [HttpPut("change-role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleDto model)
    {
        try
        {
            var user = await _context.AspNetUsers
                .FirstOrDefaultAsync(u => u.Email == model.Email || u.NormalizedEmail == model.Email.ToUpperInvariant());
            
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            // Находим роль по имени
            var role = await _context.AspNetRoles
                .FirstOrDefaultAsync(r => r.Name == model.RoleName || r.NormalizedName == model.RoleName.ToUpperInvariant());
            
            if (role == null)
                return BadRequest(new { message = $"Роль '{model.RoleName}' не найдена" });

            // Получаем текущие роли пользователя
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Удаляем все текущие роли из таблицы AspNetUserRoles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Ошибка при удалении ролей: {errors}" });
                }
            }

            // Назначаем новую роль через таблицу AspNetUserRoles
            var addResult = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Ошибка при назначении роли: {errors}" });
            }

            return Ok(new 
            { 
                message = $"Роль пользователя {user.Email} успешно изменена",
                previousRoles = currentRoles.ToList(),
                newRole = role.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при изменении роли");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _context.AspNetUsers.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var roles = await GetUserRolesAsync(user.Id);

            return Ok(new UserInfoDto
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Avatar = user.Avatar,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении текущего пользователя");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // GET: api/auth/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult> GetProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _context.AspNetUsers.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var roles = await GetUserRolesAsync(user.Id);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.City,
                user.Address,
                user.Avatar,
                user.CreatedAt,
                user.LastLoginAt,
                Roles = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении профиля");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    private async Task<string> GenerateJwtToken(AspNetUser user)
    {
        var roles = await GetUserRolesAsync(user.Id);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Получает роли пользователя напрямую из БД через SQL запрос
    /// </summary>
    private async Task<List<string>> GetUserRolesAsync(string userId)
    {
        try
        {
            // Используем UserManager для получения ролей (он работает с Identity)
            var user = await _context.AspNetUsers.FindAsync(userId);
            if (user == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }
        catch
        {
            // Если UserManager не работает, используем прямой SQL запрос
            // Проверяем состояние соединения перед открытием
            var roleNames = new List<string>();
            var connection = _context.Database.GetDbConnection();
            
            // Проверяем, открыто ли соединение
            var wasOpen = connection.State == ConnectionState.Open;
            
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT r.""Name"" 
                    FROM ""AspNetUserRoles"" ur
                    INNER JOIN ""AspNetRoles"" r ON ur.""RoleId"" = r.""Id""
                    WHERE ur.""UserId"" = @userId";
                
                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@userId";
                userIdParam.Value = userId;
                command.Parameters.Add(userIdParam);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    roleNames.Add(reader.GetString(0));
                }
            }
            finally
            {
                // Закрываем соединение только если мы его открывали
                if (!wasOpen && connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
            
            return roleNames;
        }
    }
}

public class AssignRoleDto
{
    public string Email { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}

public class ChangeRoleDto
{
    public string Email { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}
