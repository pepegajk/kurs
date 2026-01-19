using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using kursecond1.Models;

namespace kursecond1.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private UserInfoDto? _currentUser;
    private const string TokenKey = "authToken";

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public UserInfoDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public bool IsAdmin => (_currentUser?.Roles.Contains("Admin") ?? false) || 
                           (_currentUser?.Roles.Contains("Administrator") ?? false);
    public bool IsManager => (_currentUser?.Roles.Contains("Manager") ?? false) || IsAdmin;
    public bool IsSeller => (_currentUser?.Roles.Contains("Dealer") ?? false) || IsManager || IsAdmin;
    public bool IsUser => (_currentUser?.Roles.Contains("User") ?? false) || IsManager || IsSeller;

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    await SaveTokenAsync(authResponse.Token);
                    
                    _currentUser = new UserInfoDto
                    {
                        UserId = authResponse.UserId,
                        UserName = authResponse.UserName,
                        Email = authResponse.Email,
                        FirstName = authResponse.FirstName,
                        LastName = authResponse.LastName,
                        Roles = authResponse.Roles
                    };

                    NotifyAuthStateChanged();
                    return (true, null);
                }
            }
            else
            {
                // Пытаемся извлечь сообщение об ошибке из ответа
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (errorResponse.TryGetProperty("message", out var messageElement))
                    {
                        var errorMessage = messageElement.GetString();
                        return (false, errorMessage);
                    }
                }
                catch
                {
                    // Если не удалось распарсить, возвращаем общее сообщение
                }
            }

            return (false, "Неверный Email или пароль");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return (false, $"Произошла ошибка: {ex.Message}");
        }
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerDto);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    await SaveTokenAsync(authResponse.Token);
                    
                    _currentUser = new UserInfoDto
                    {
                        UserId = authResponse.UserId,
                        UserName = authResponse.UserName,
                        Email = authResponse.Email,
                        FirstName = authResponse.FirstName,
                        LastName = authResponse.LastName,
                        Roles = authResponse.Roles
                    };

                    NotifyAuthStateChanged();
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await RemoveTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        NotifyAuthStateChanged();
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<UserInfoDto>();
                NotifyAuthStateChanged();
                return true;
            }
            else
            {
                await RemoveTokenAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialize error: {ex.Message}");
            await RemoveTokenAsync();
            return false;
        }
    }

    private async Task SaveTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"Token saved to localStorage and HttpClient. Token length: {token.Length}");
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }

    private async Task RemoveTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
        catch
        {
            // Игнорируем ошибки при удалении токена
        }
    }

    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }
}

