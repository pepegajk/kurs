using kursecond1;
using kursecond1.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API - используем Singleton для единого экземпляра во всем приложении
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7280/") });

// Register services - важно: AuthService должен быть зарегистрирован первым
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddScoped<HotkeyService>();
builder.Services.AddScoped<ExportService>();

var host = builder.Build();

// Инициализация AuthService при старте приложения
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();
