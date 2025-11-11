using kursecond1.Models;

namespace kursecond1.Services;

public class UserSettingsService
{
    private UserSettingsDto _settings = new();
    public event Action? OnSettingsChanged;

    public UserSettingsDto GetSettings() => _settings;

    public void UpdateSettings(UserSettingsDto settings)
    {
        _settings = settings;
        OnSettingsChanged?.Invoke();
    }

    public string FormatDate(DateTime date)
    {
        return _settings.DateFormat switch
        {
            "dd.MM.yyyy" => date.ToString("dd.MM.yyyy"),
            "MM/dd/yyyy" => date.ToString("MM/dd/yyyy"),
            "yyyy-MM-dd" => date.ToString("yyyy-MM-dd"),
            _ => date.ToString("dd.MM.yyyy")
        };
    }

    public string FormatPrice(decimal price)
    {
        return _settings.Currency switch
        {
            "RUB" => $"{price:N0} ₽",
            "USD" => $"${price:N2}",
            "EUR" => $"€{price:N2}",
            _ => $"{price:N0} ₽"
        };
    }
}

