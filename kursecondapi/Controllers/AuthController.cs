using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using kursecondapi.Models;
using kursecondapi.DTOs;

namespace kursecondapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AspNetUser> _userManager;
    private readonly SignInManager<AspNetUser> _signInManager;
    private readonly RoleManager<AspNetRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AspNetUser> userManager,
        SignInManager<AspNetUser> signInManager,
        RoleManager<AspNetRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "Пользователь с таким email уже существует" });

            var user = new AspNetUser
            {
                Id = Guid.NewGuid().ToString(), // Генерируем UUID для пользователя
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                City = model.City,
                Address = model.Address,
                CreatedAt = DateTime.Now, // Используем локальное время вместо UTC
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Ошибка при создании пользователя: {errors}" });
            }

            // Назначаем роль User по умолчанию
            await _userManager.AddToRoleAsync(user, "User");

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
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
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Неверный email или пароль" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Ваш аккаунт деактивирован" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Неверный email или пароль" });

            // Обновляем LastLoginAt
            user.LastLoginAt = DateTime.Now; // Используем локальное время
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserInfoDto
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var roles = await _userManager.GetRolesAsync(user);

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
        var roles = await _userManager.GetRolesAsync(user);
        
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
}

public class AssignRoleDto
{
    public string Email { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}
