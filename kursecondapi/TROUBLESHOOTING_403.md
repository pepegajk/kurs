# Решение проблемы 403 Forbidden для Менеджера

## ✅ Что было сделано

1. **Добавлено детальное логирование** в `DealsController` и `StatisticsController`
2. **Создан middleware** `AuthorizationLoggingMiddleware` для логирования всех запросов с авторизацией
3. **Все эндпоинты проверены** - они используют правильную авторизацию

## 🔍 Диагностика

### Шаг 1: Перезапустить API сервер

**ОБЯЗАТЕЛЬНО** перезапустите API сервер после изменений:

```bash
# Остановить сервер (Ctrl+C)
# Запустить заново
dotnet run
```

### Шаг 2: Проверить логи API

После перезапуска попробуйте зайти на страницу сделок. В логах API вы должны увидеть:

```
API Request: GET /api/Deals | User: {userId} | Roles: [Manager] | All Claims: [...]
```

**Если в логах роли пустые или нет "Manager":**
- Проблема в токене - роль не включена
- Нужно проверить токен на jwt.io

### Шаг 3: Проверить токен на jwt.io

1. Откройте DevTools (F12) → Console
2. Выполните: `localStorage.getItem('authToken')`
3. Скопируйте токен
4. Откройте https://jwt.io
5. Вставьте токен в поле "Encoded"
6. Проверьте раздел "Payload"

**Ожидаемый результат:**
В payload должен быть claim:
```json
{
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Manager"
}
```

**Если роли нет:**
- Роль не назначена пользователю в БД
- Или пользователь не перелогинился после назначения роли

### Шаг 4: Проверить роль в базе данных

Выполните SQL:
```sql
-- Проверить, что роль Manager существует
SELECT * FROM "AspNetRoles" WHERE "Name" = 'Manager';

-- Проверить, что у пользователя есть роль Manager
SELECT 
    u."Email",
    u."Id" as UserId,
    r."Name" as RoleName
FROM "AspNetUsers" u
INNER JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'email_менеджера@example.com';
```

**Должна быть строка с `RoleName = 'Manager'`**

### Шаг 5: Назначить роль (если отсутствует)

#### Вариант 1: Через API (если есть доступ админа)
```http
POST /api/auth/assign-role
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "email": "manager@example.com",
  "roleName": "Manager"
}
```

#### Вариант 2: Через SQL
```sql
-- Найти ID пользователя и роли
SELECT u."Id" as UserId, r."Id" as RoleId
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'manager@example.com' AND r."Name" = 'Manager';

-- Вставить связь (замените на реальные ID)
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('user-id-here', 'role-id-here');
```

**После назначения роли:**
- Пользователь должен **выйти и войти заново**, чтобы получить новый токен с ролью

---

## 📋 Чеклист

- [ ] API сервер перезапущен после изменений
- [ ] Проверены логи API - какие роли видны?
- [ ] Проверен токен на jwt.io - есть ли роль "Manager"?
- [ ] Проверена база данных - есть ли роль "Manager" у пользователя?
- [ ] Пользователь вышел и вошел заново после назначения роли
- [ ] Очищен кэш браузера

---

## 🐛 Проблема с изображениями (404)

Клиент обращается к `/api/CarImages/car/{id}/main`, но получает 404.

**Возможные причины:**
1. Изображения не загружены для этих автомобилей
2. URL неправильный (регистр букв)

**Проверка:**
- Убедитесь, что в базе данных есть записи в таблице `CarImages` для этих автомобилей
- Проверьте, что URL правильный (должен быть `/api/CarImages/car/{id}/main` с заглавной C)

---

## 📞 Если ничего не помогло

Пришлите:
1. **Логи API** - строки с "API Request" из middleware
2. **Скриншот payload токена** с jwt.io (показывающий все claims)
3. **Результат SQL запроса** для проверки ролей пользователя
4. **Email пользователя**, который получает 403
