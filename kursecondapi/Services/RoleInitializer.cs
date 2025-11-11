using Microsoft.AspNetCore.Identity;
using kursecondapi.Models;

namespace kursecondapi.Services;

public static class RoleInitializer
{
    public static async Task InitializeAsync(UserManager<AspNetUser> userManager, RoleManager<AspNetRole> roleManager)
    {
        // Роли уже существуют в БД, проверяем их наличие
        // В БД есть: Administrator, Dealer, User, Manager
        
        // Создание админа по умолчанию (если нужно)
        var adminEmail = "admin@carplatform.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var admin = new AspNetUser
            {
                Id = Guid.NewGuid().ToString(), // Генерируем UUID для пользователя
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123");

            if (result.Succeeded)
            {
                // Используем роль "Administrator" из существующей БД
                var adminRoleExists = await roleManager.RoleExistsAsync("Administrator");
                if (adminRoleExists)
                {
                    await userManager.AddToRoleAsync(admin, "Administrator");
                }
            }
        }
    }
}

