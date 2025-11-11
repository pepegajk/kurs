using System.Net.Http.Json;
using kursecond1.Models;

namespace kursecond1.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            return await _httpClient.GetFromJsonAsync<List<CarDto>>("api/cars") ?? new List<CarDto>();
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
            return await _httpClient.GetFromJsonAsync<List<DealDto>>("api/deals") ?? new List<DealDto>();
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
            return await _httpClient.GetFromJsonAsync<DealDto>($"api/deals/{id}");
        }
        catch
        {
            return null;
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
}

