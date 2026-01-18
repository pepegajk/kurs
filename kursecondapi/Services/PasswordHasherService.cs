using System.Security.Cryptography;
using System.Text;

namespace kursecondapi.Services;

public class PasswordHasherService
{
    /// <summary>
    /// Хеширует пароль используя SHA-256
    /// </summary>
    /// <param name="password">Не хешированный пароль</param>
    /// <returns>Хеш пароля в формате hex строки</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        using (var sha256 = SHA256.Create())
        {
            // Конвертируем пароль в байты
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            
            // Вычисляем хеш
            var hashBytes = sha256.ComputeHash(passwordBytes);
            
            // Конвертируем хеш в hex строку
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            
            return hashString;
        }
    }

    /// <summary>
    /// Сравнивает не хешированный пароль с хешем из базы данных
    /// </summary>
    /// <param name="password">Не хешированный пароль</param>
    /// <param name="hashedPassword">Хеш пароля из базы данных</param>
    /// <returns>True если пароли совпадают, иначе False</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        // Хешируем входящий пароль
        var passwordHash = HashPassword(password);
        
        // Сравниваем хеши (безопасное сравнение строк)
        return SecureCompare(passwordHash, hashedPassword);
    }

    /// <summary>
    /// Безопасное сравнение строк (защита от timing attacks)
    /// </summary>
    private bool SecureCompare(string a, string b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}


