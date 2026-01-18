using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using kursecondapi.Models;

namespace kursecondapi.Services;

public static class RoleInitializer
{
    public static async Task InitializeAsync(
        CarPlatformContext context, 
        PasswordHasherService passwordHasher,
        UserManager<AspNetUser> userManager, 
        RoleManager<AspNetRole> roleManager)
    {
        // Роли уже существуют в БД, проверяем их наличие
        // В БД есть: Administrator, Dealer, User, Manager
        
        // Создание админа по умолчанию (если нужно)
        var adminEmail = "admin@carplatform.com";
        var adminUser = await context.AspNetUsers
            .FirstOrDefaultAsync(u => u.Email == adminEmail || u.NormalizedEmail == adminEmail.ToUpperInvariant());

        if (adminUser == null)
        {
            // Хешируем пароль используя SHA-256
            var hashedPassword = passwordHasher.HashPassword("Admin123");

            var admin = new AspNetUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpperInvariant(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                PasswordHash = hashedPassword, // Сохраняем SHA-256 хеш
                SecurityStamp = Guid.NewGuid().ToString(), // Обязательное поле для Identity
                ConcurrencyStamp = Guid.NewGuid().ToString(), // Обязательное поле для Identity
                FirstName = "Admin",
                LastName = "Administrator",
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                CreatedAt = DateTime.Now,
                IsActive = true,
                LockoutEnabled = true,
                LockoutEnd = null,
                TwoFactorEnabled = false,
                AccessFailedCount = 0
            };

            context.AspNetUsers.Add(admin);
            await context.SaveChangesAsync();

            // Используем роль "Administrator" из существующей БД
            var adminRole = await context.AspNetRoles.FirstOrDefaultAsync(r => r.Name == "Administrator");
            if (adminRole != null)
            {
                // Добавляем связь пользователь-роль в промежуточную таблицу
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"AspNetUserRoles\" (\"UserId\", \"RoleId\") VALUES ({0}, {1})",
                    admin.Id, adminRole.Id);
                await context.SaveChangesAsync();
            }
        }
    }
}

