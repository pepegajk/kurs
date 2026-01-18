namespace kursecond1.Models;

public class UserSettingsDto
{
    public string UserId { get; set; } = "demo-user";
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "ru";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public string TimeFormat { get; set; } = "HH:mm";
    public string Currency { get; set; } = "RUB";
    public int PageSize { get; set; } = 20;
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = false;
}

