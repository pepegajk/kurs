using kursecond1;
using kursecond1.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API - используем Singleton для общего HttpClient
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7280/") };
builder.Services.AddSingleton(httpClient);

// Register services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddScoped<HotkeyService>();
builder.Services.AddScoped<ExportService>();

var host = builder.Build();

// Инициализация AuthService при старте приложения
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();
