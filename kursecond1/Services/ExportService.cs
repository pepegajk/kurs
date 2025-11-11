using System.Text;
using kursecond1.Models;

namespace kursecond1.Services;

public class ExportService
{
    public byte[] ExportCarsToCsv(List<CarDto> cars)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("ID,Brand,Model,Year,Price,Mileage,Color,FuelType,Transmission,Location,Status,VIN");
        
        // Data
        foreach (var car in cars)
        {
            sb.AppendLine($"{car.Id},{Escape(car.BrandName)},{Escape(car.ModelName)},{car.Year}," +
                         $"{car.Price},{car.Mileage},{Escape(car.Color)},{Escape(car.FuelType)}," +
                         $"{Escape(car.Transmission)},{Escape(car.Location)},{Escape(car.Status)},{Escape(car.VIN)}");
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportDealsToCsv(List<DealDto> deals)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("ID,CarInfo,Seller,Buyer,Price,Commission,Status,CreatedAt,CompletedAt");
        
        // Data
        foreach (var deal in deals)
        {
            sb.AppendLine($"{deal.Id},{Escape(deal.CarInfo)},{Escape(deal.SellerName)},{Escape(deal.BuyerName)}," +
                         $"{deal.Price},{deal.CommissionAmount?.ToString() ?? ""}," +
                         $"{Escape(deal.Status)},{deal.CreatedAt:yyyy-MM-dd},{deal.CompletedAt?.ToString("yyyy-MM-dd") ?? ""}");
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportBrandsToCsv(List<BrandDto> brands)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("ID,Name,Country,Description,IsActive");
        
        // Data
        foreach (var brand in brands)
        {
            sb.AppendLine($"{brand.Id},{Escape(brand.Name)},{Escape(brand.Country)}," +
                         $"{Escape(brand.Description)},{brand.IsActive}");
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}

