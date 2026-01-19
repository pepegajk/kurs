using System.Security.Claims;

namespace kursecondapi.Middleware;

public class AuthorizationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

    public AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Логируем только для запросов к API с авторизацией
        if (context.Request.Path.StartsWithSegments("/api") && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            
            var allClaims = context.User.Claims
                .Select(c => $"{c.Type}={c.Value}")
                .ToList();

            _logger.LogWarning(
                "API Request: {Method} {Path} | User: {UserId} | Roles: [{Roles}] | All Claims: [{AllClaims}]",
                context.Request.Method,
                context.Request.Path,
                userId ?? "null",
                string.Join(", ", userRoles),
                string.Join("; ", allClaims)
            );
        }

        await _next(context);
    }
}
