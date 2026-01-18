using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using kursecond1.Models;

namespace kursecond1.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string ApiBaseUrl = "https://localhost:7280";

    public ApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    private async Task EnsureTokenAsync()
    {
        try
        {
            Console.WriteLine("EnsureTokenAsync called");
            var token = await _authService.GetTokenAsync();
            Console.WriteLine($"Token retrieved: {(string.IsNullOrEmpty(token) ? "NULL or EMPTY" : $"Found, length: {token.Length}")}");
            
            if (!string.IsNullOrEmpty(token))
            {
                // Всегда обновляем токен (на случай если он изменился)
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
                
                // Проверяем, что токен установлен
                var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
                if (authHeader != null)
                {
                    Console.WriteLine($"Token successfully set in HttpClient. Scheme: {authHeader.Scheme}, Parameter length: {authHeader.Parameter?.Length ?? 0}");
                }
                else
                {
                    Console.WriteLine("ERROR: Token was NOT set in HttpClient!");
                }
            }
            else
            {
                Console.WriteLine("WARNING: No token available. User may not be authenticated. Please login first.");
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in EnsureTokenAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
        }
    }

    /// <summary>
    /// Преобразует относительный URL изображения в полный URL к API
    /// </summary>
    public string GetImageUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return string.Empty;

        // Если уже полный URL, возвращаем как есть
        if (relativeUrl.StartsWith("http://") || relativeUrl.StartsWith("https://"))
            return relativeUrl;

        // Убираем начальный слеш если есть
        var cleanUrl = relativeUrl.TrimStart('/');
        
        // Формируем полный URL к API
        return $"{ApiBaseUrl}/{cleanUrl}";
    }

    // Brands
    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<BrandDto>>("api/brands") ?? new List<BrandDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching brands: {ex.Message}");
            return new List<BrandDto>();
        }
    }

    public async Task<BrandDto?> GetBrandByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BrandDto>($"api/brands/{id}");
        }
        catch
        {
            return null;
        }
    }

    // Models
    public async Task<List<ModelDto>> GetModelsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ModelDto>>("api/models") ?? new List<ModelDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching models: {ex.Message}");
            return new List<ModelDto>();
        }
    }

    public async Task<List<ModelDto>> GetModelsByBrandAsync(int brandId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ModelDto>>($"api/models/brand/{brandId}") ?? new List<ModelDto>();
        }
        catch
        {
            return new List<ModelDto>();
        }
    }

    // Cars
    public async Task<List<CarDto>> GetCarsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/cars");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var cars = JsonSerializer.Deserialize<List<CarDto>>(jsonString, _jsonOptions) ?? new List<CarDto>();
                return cars;
            }
            return new List<CarDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching cars: {ex.Message}");
            return new List<CarDto>();
        }
    }

    public async Task<CarDto?> GetCarByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CarDto>($"api/cars/{id}");
        }
        catch
        {
            return null;
        }
    }

    // Deals
    public async Task<List<DealDto>> GetDealsAsync()
    {
        try
        {
            await EnsureTokenAsync();
            return await _httpClient.GetFromJsonAsync<List<DealDto>>("api/Deals") ?? new List<DealDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching deals: {ex.Message}");
            return new List<DealDto>();
        }
    }

    public async Task<DealDto?> GetDealByIdAsync(int id)
    {
        try
        {
            await EnsureTokenAsync();
            return await _httpClient.GetFromJsonAsync<DealDto>($"api/Deals/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<DealDto>> GetMyDealsAsync()
    {
        try
        {
            await EnsureTokenAsync();
            Console.WriteLine("GetMyDealsAsync: Calling api/Deals/my");
            var deals = await _httpClient.GetFromJsonAsync<List<DealDto>>("api/Deals/my") ?? new List<DealDto>();
            Console.WriteLine($"GetMyDealsAsync: Received {deals.Count} deals");
            return deals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching my deals: {ex.Message}");
            return new List<DealDto>();
        }
    }

    public async Task<bool> CreateDealAsync(int carId, decimal price)
    {
        try
        {
            Console.WriteLine($"CreateDealAsync called: carId={carId}, price={price}");
            
            await EnsureTokenAsync();
            
            // Проверяем заголовки перед отправкой
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            Console.WriteLine($"Before POST request - Auth header: {(authHeader != null ? $"Scheme={authHeader.Scheme}, HasParameter={!string.IsNullOrEmpty(authHeader.Parameter)}" : "NULL")}");
            
            // Получаем BuyerId из AuthService
            string? buyerId = null;
            if (_authService.CurrentUser != null)
            {
                buyerId = _authService.CurrentUser.UserId;
                Console.WriteLine($"BuyerId from AuthService: {buyerId}");
            }
            else
            {
                Console.WriteLine("WARNING: CurrentUser is NULL in AuthService!");
            }
            
            // Получаем информацию об автомобиле (для логирования)
            var car = await GetCarByIdAsync(carId);
            if (car != null)
            {
                Console.WriteLine($"Car info: SellerId={car.SellerId}");
            }
            else
            {
                Console.WriteLine("WARNING: Could not fetch car information!");
            }
            
            // Создаем заказ - НЕ отправляем SellerId и BuyerId,
            // API должен автоматически установить их из токена и автомобиля
            // Также пробуем минимальный набор полей
            var deal = new DealDto
            {
                CarId = carId,
                Price = price,
                Status = "Created" // Статус заказа
                // BuyerId и SellerId НЕ устанавливаем - API должен сам их определить
            };
            
            Console.WriteLine("NOTE: BuyerId and SellerId are NOT sent - API should set them automatically");
            
            Console.WriteLine($"Sending POST request to api/Deals with deal:");
            Console.WriteLine($"  CarId={deal.CarId}");
            Console.WriteLine($"  Price={deal.Price}");
            Console.WriteLine($"  Status={deal.Status}");
            Console.WriteLine($"  BuyerId={deal.BuyerId}");
            Console.WriteLine($"  SellerId={deal.SellerId}");
            
            var response = await _httpClient.PostAsJsonAsync("api/Deals", deal);
            
            Console.WriteLine($"Response received: StatusCode={response.StatusCode}, IsSuccessStatusCode={response.IsSuccessStatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"CreateDeal failed: {response.StatusCode} - {errorContent}");
                Console.WriteLine($"Full error response: {errorContent}");
                Console.WriteLine($"Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                
                // Попытка распарсить детальную ошибку если она есть
                try
                {
                    if (!string.IsNullOrEmpty(errorContent))
                    {
                        var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
                        
                        // Выводим все свойства ошибки
                        Console.WriteLine("=== ERROR DETAILS ===");
                        foreach (var prop in errorJson.EnumerateObject())
                        {
                            Console.WriteLine($"  {prop.Name}: {prop.Value}");
                        }
                        
                        if (errorJson.TryGetProperty("errors", out var errors))
                        {
                            Console.WriteLine("Validation errors:");
                            foreach (var error in errors.EnumerateObject())
                            {
                                Console.WriteLine($"  {error.Name}: {string.Join(", ", error.Value.EnumerateArray().Select(e => e.GetString()))}");
                            }
                        }
                        if (errorJson.TryGetProperty("message", out var message))
                        {
                            Console.WriteLine($"Error message: {message.GetString()}");
                        }
                        if (errorJson.TryGetProperty("title", out var title))
                        {
                            Console.WriteLine($"Error title: {title.GetString()}");
                        }
                        if (errorJson.TryGetProperty("detail", out var detail))
                        {
                            Console.WriteLine($"Error detail: {detail.GetString()}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Response body is empty!");
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"Could not parse error response as JSON: {parseEx.Message}");
                    Console.WriteLine($"Raw error content: {errorContent}");
                }
                
                // Дополнительная информация о 403 ошибке
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("=== 403 FORBIDDEN ANALYSIS ===");
                    Console.WriteLine("Possible reasons:");
                    Console.WriteLine("  1. User does not have permission to create deals");
                    Console.WriteLine("  2. User role 'User' cannot create deals (may need 'Manager' or 'Admin')");
                    Console.WriteLine("  3. API authorization policy requires specific role");
                    Console.WriteLine("  4. User cannot create deal for their own car (buyer == seller)");
                }
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating deal: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> UpdateDealStatusAsync(int dealId, string status)
    {
        try
        {
            await EnsureTokenAsync();
            
            // Получаем текущую сделку
            var deal = await GetDealByIdAsync(dealId);
            if (deal == null)
                return false;

            // Обновляем статус
            deal.Status = status;
            if (status == "Completed")
            {
                deal.CompletedAt = DateTime.UtcNow;
            }

            var response = await _httpClient.PutAsJsonAsync($"api/Deals/{dealId}", deal);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"UpdateDealStatus failed: {response.StatusCode} - {errorContent}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating deal status: {ex.Message}");
            return false;
        }
    }

    public async Task<List<DealDto>> GetDealsByCarIdAsync(int carId)
    {
        try
        {
            await EnsureTokenAsync();
            return await _httpClient.GetFromJsonAsync<List<DealDto>>($"api/Deals/car/{carId}") ?? new List<DealDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching deals by car id: {ex.Message}");
            return new List<DealDto>();
        }
    }

    // Получить все уникальные статусы из существующих заказов
    public async Task<List<string>> GetDealStatusesAsync()
    {
        try
        {
            var deals = await GetDealsAsync();
            return deals.Select(d => d.Status)
                       .Distinct()
                       .OrderBy(s => s)
                       .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching deal statuses: {ex.Message}");
            // Возвращаем стандартные статусы по умолчанию
            return new List<string> { "Pending", "Completed", "Cancelled", "Created", "UnderReview", "InProgress" };
        }
    }

    // Statistics
    public async Task<StatisticsDto?> GetStatisticsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StatisticsDto>("api/statistics");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching statistics: {ex.Message}");
            return null;
        }
    }

    // Export
    public async Task<byte[]?> ExportCarsToCSVAsync()
    {
        try
        {
            return await _httpClient.GetByteArrayAsync("api/manager/export/cars");
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportDealsToCSVAsync()
    {
        try
        {
            return await _httpClient.GetByteArrayAsync("api/manager/export/deals");
        }
        catch
        {
            return null;
        }
    }

    // Generic GET method
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GET {endpoint}: {ex.Message}");
            return default;
        }
    }

    // Generic POST method
    public async Task<bool> PostAsync<T>(string endpoint, T? data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in POST {endpoint}: {ex.Message}");
            return false;
        }
    }

    // Admin methods - Brands
    public async Task<bool> CreateBrandAsync(BrandDto brand)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/brands", brand);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating brand: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateBrandAsync(int id, BrandDto brand)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/brands/{id}", brand);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating brand: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteBrandAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/brands/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting brand: {ex.Message}");
            return false;
        }
    }

    // Admin methods - Models
    public async Task<bool> CreateModelAsync(ModelDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/models", model);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating model: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateModelAsync(int id, ModelDto model)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/models/{id}", model);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating model: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteModelAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/models/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting model: {ex.Message}");
            return false;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ModelDto>($"api/models/{id}");
        }
        catch
        {
            return null;
        }
    }

    // Admin methods - Cars
    public async Task<bool> CreateCarAsync(CarDto car)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/cars", car);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating car: {ex.Message}");
            return false;
        }
    }

    // Deals methods
    public async Task<bool> CreateDealAsync(DealDto deal)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/deals", deal);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating deal: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateDealAsync(int id, DealDto deal)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/deals/{id}", deal);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating deal: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateCarAsync(int id, CarDto car)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/cars/{id}", car);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating car: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCarAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/cars/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting car: {ex.Message}");
            return false;
        }
    }

    // Admin methods - Users
    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<AdminUserDto>>("api/admin/users") ?? new List<AdminUserDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users: {ex.Message}");
            return new List<AdminUserDto>();
        }
    }

    public async Task<bool> ToggleUserActiveAsync(string userId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/admin/users/{userId}/toggle-active", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling user active: {ex.Message}");
            return false;
        }
    }

    // Car Images methods
    public async Task<List<CarImageDto>> GetCarImagesAsync(int carId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CarImageDto>>($"api/CarImages/car/{carId}") ?? new List<CarImageDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching car images: {ex.Message}");
            return new List<CarImageDto>();
        }
    }

    public async Task<bool> CreateCarImageAsync(CarImageDto image)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/CarImages", image);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating car image: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCarImageAsync(int carId, int imageId)
    {
        try
        {
            // Используем правильный формат из Swagger: DELETE /api/CarImages/{id}
            var response = await _httpClient.DeleteAsync($"api/CarImages/{imageId}");
            
            if (!response.IsSuccessStatusCode)
            {
                // Логируем детали ошибки для отладки
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Delete failed: {response.StatusCode} - {errorContent}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting car image: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetMainImageAsync(int imageId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/CarImages/{imageId}/setmain", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting main image: {ex.Message}");
            return false;
        }
    }

    public async Task<CarImageDto?> GetMainCarImageAsync(int carId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CarImageDto>($"api/CarImages/car/{carId}/main");
        }
        catch
        {
            return null;
        }
    }

    // Upload image file
    public async Task<bool> UploadCarImageFileAsync(int carId, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(carId.ToString()), "carId");

            var response = await _httpClient.PostAsync("api/CarImages/upload", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading car image file: {ex.Message}");
            return false;
        }
    }
}

