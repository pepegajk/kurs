using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO;
using System.Security.Claims;
using kursecondapi.Models;
using kursecondapi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Подключение к PostgreSQL
builder.Services.AddDbContext<CarPlatformContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервиса хеширования паролей
builder.Services.AddScoped<PasswordHasherService>();

// Настройка Identity
builder.Services.AddIdentity<AspNetUser, AspNetRole>(options =>
{
    // Настройки пароля
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Настройки пользователя
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<CarPlatformContext>()
.AddDefaultTokenProviders();

// Настройка JWT аутентификации
var jwtKey = builder.Configuration["Jwt:Key"]!;
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero,
        // Явно указываем тип claim для ролей
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
});

// Настройка политик авторизации
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "Administrator"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager", "Admin", "Administrator"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "Manager", "Admin", "Administrator"));
});

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Authorization"); // Разрешаем клиенту читать заголовок Authorization
    });
});

builder.Services.AddControllers();

// Настройка для загрузки файлов (увеличение лимита до 50 МБ)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 МБ
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Car Platform API", 
        Version = "v1" 
    });
    
    // Настройка JWT авторизации в Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Настройка для загрузки файлов в Swagger
    c.OperationFilter<kursecondapi.Swagger.FileUploadOperationFilter>();
});

var app = builder.Build();

// Создание папки wwwroot если её нет
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
    Directory.CreateDirectory(Path.Combine(wwwrootPath, "images", "cars"));
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Поддержка статических файлов (wwwroot)
app.UseStaticFiles();

// Применение CORS (должно быть перед UseAuthorization)
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Инициализация ролей
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CarPlatformContext>();
        var passwordHasher = services.GetRequiredService<PasswordHasherService>();
        var userManager = services.GetRequiredService<UserManager<AspNetUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AspNetRole>>();
        await RoleInitializer.InitializeAsync(context, passwordHasher, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при инициализации ролей");
    }
}

app.Run();
