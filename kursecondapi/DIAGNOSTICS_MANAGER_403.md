# Диагностика проблемы 403 Forbidden для Менеджера

## ✅ Проверка 1: Код API - ВСЕ ПРАВИЛЬНО

### DealsController
- ✅ `GET /api/deals` - `[Authorize(Policy = "ManagerOnly")]`
- ✅ `PUT /api/deals/{id}` - `[Authorize(Policy = "ManagerOnly")]`
- ✅ `POST /api/deals/{id}/approve` - `[Authorize(Roles = "Manager,Admin,Administrator")]`

### StatisticsController
- ✅ Весь контроллер - `[Authorize(Policy = "ManagerOnly")]` на уровне класса

### Политика в Program.cs
```csharp
options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager", "Admin", "Administrator"));
```
✅ Политика правильно настроена и включает роль "Manager"

### Генерация JWT токена
```csharp
var roles = await GetUserRolesAsync(user.Id);
claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
```
✅ Роли правильно добавляются в токен

---

## 🔍 Что проверить дальше

### 1. Проверить токен на jwt.io

**Шаги:**
1. Открыть DevTools (F12) в браузере
2. Console → ввести: `localStorage.getItem('authToken')`
3. Скопировать токен
4. Перейти на https://jwt.io
5. Вставить токен в поле "Encoded"
6. Проверить в разделе "Payload" → найти claim с типом `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"` или просто `"role"`

**Ожидаемый результат:**
В payload должно быть что-то вроде:
```json
{
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Manager"
}
```
или
```json
{
  "role": "Manager"
}
```

**Если роли нет в токене:**
- Проблема в генерации токена
- Нужно проверить, что роль "Manager" назначена пользователю в БД
- Нужно проверить метод `GetUserRolesAsync` в `AuthController`

---

### 2. Проверить роль в базе данных

**SQL запрос:**
```sql
-- Проверить, что роль Manager существует
SELECT * FROM "AspNetRoles" WHERE "Name" = 'Manager';

-- Проверить, что у пользователя есть роль Manager
SELECT 
    u."Email",
    u."Id" as UserId,
    r."Name" as RoleName,
    r."Id" as RoleId
FROM "AspNetUsers" u
INNER JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'email_менеджера@example.com';
```

**Ожидаемый результат:**
Должна быть строка с `RoleName = 'Manager'`

**Если роли нет:**
- Нужно назначить роль пользователю через API или SQL

---

### 3. Проверить логи API

**После перезапуска API сервера:**
1. Попробовать зайти на страницу сделок как менеджер
2. Посмотреть логи в терминале API
3. Должны быть строки:
   ```
   GetDeals вызван пользователем {UserId} с ролями: Manager
   GetStatistics вызван пользователем {UserId} с ролями: Manager
   ```

**Если в логах роли пустые или нет роли Manager:**
- Проблема в токене - роль не включена
- Нужно перегенерировать токен (выйти и войти заново)

---

### 4. Проверить, что API сервер перезапущен

**Важно:** После изменения кода API обязательно нужно перезапустить сервер!

**Шаги:**
1. Остановить API сервер (Ctrl+C в терминале)
2. Запустить заново: `dotnet run` или через IDE
3. Проверить, что сервер запустился без ошибок

---

## 🛠️ Решение проблем

### Проблема: Роль не в токене

**Решение 1: Назначить роль через API**
```http
POST /api/auth/assign-role
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "email": "manager@example.com",
  "roleName": "Manager"
}
```

**Решение 2: Назначить роль через SQL**
```sql
-- Найти ID пользователя и роли
SELECT u."Id" as UserId, r."Id" as RoleId
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'manager@example.com' AND r."Name" = 'Manager';

-- Вставить связь (замените UserId и RoleId на реальные значения)
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('user-id-here', 'role-id-here');
```

**После назначения роли:**
- Пользователь должен выйти и войти заново, чтобы получить новый токен с ролью

---

### Проблема: Политика не работает

**Проверка:**
1. Убедиться, что в `Program.cs` политика настроена правильно
2. Убедиться, что `RoleClaimType = ClaimTypes.Role` в настройках JWT

**Если не помогает:**
- Попробовать использовать прямой атрибут вместо политики:
  ```csharp
  [Authorize(Roles = "Manager,Admin,Administrator")]
  ```

---

### Проблема: Кэш браузера

**Решение:**
1. Очистить кэш браузера (Ctrl+Shift+Delete)
2. Или открыть в режиме инкогнито (Ctrl+Shift+N)
3. Войти заново как менеджер

---

## 📋 Чеклист для пользователя

- [ ] API сервер перезапущен после изменений
- [ ] Проверен токен на jwt.io - есть ли роль "Manager"?
- [ ] Проверена база данных - есть ли роль "Manager" у пользователя?
- [ ] Пользователь вышел и вошел заново после назначения роли
- [ ] Очищен кэш браузера
- [ ] Проверены логи API - какие роли видны в логах?

---

## 📞 Если ничего не помогло

Пришлите:
1. Скриншот payload токена с jwt.io (показывающий все claims)
2. Результат SQL запроса для проверки ролей пользователя
3. Логи API при попытке доступа (строки с "GetDeals вызван" и "GetStatistics вызван")
4. Код метода `GetUserRolesAsync` из `AuthController.cs` (если отличается от стандартного)
