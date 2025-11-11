# 🔐 API Документация - Система ролей Car Platform

## 📋 Роли и их права

### 1. 👤 User (Пользователь)
**Доступ:**
- Просмотр автомобилей (все GET эндпоинты)
- Просмотр брендов и моделей
- Написание отзывов (после покупки)
- Добавление в избранное
- Просмотр своего профиля

### 2. 📊 Manager (Менеджер)
**Доступ ко всему что у User +**
- Добавление новых автомобилей
- Просмотр статистики (графики просмотров, продаж)
- Экспорт данных в CSV
- Панель менеджера с аналитикой
- Просмотр популярных брендов

### 3. 👑 Admin (Администратор)
**Доступ ко всему что у Manager +**
- Полный CRUD для всех сущностей
- Модерация отзывов (одобрение/отклонение)
- Управление пользователями (активация/деактивация)
- Назначение ролей пользователям
- Панель администратора с полной статистикой

---

## 🔑 Аутентификация

### POST /api/auth/register
Регистрация нового пользователя (роль User по умолчанию)

**Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123",
  "firstName": "Иван",
  "lastName": "Иванов",
  "phoneNumber": "+79001234567",
  "city": "Москва",
  "address": "ул. Примерная, 1"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "firstName": "Иван",
  "lastName": "Иванов",
  "roles": ["User"],
  "expiresAt": "2025-11-12T10:00:00Z"
}
```

### POST /api/auth/login
Вход в систему

**Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

**Response:** Такой же как при регистрации

### GET /api/auth/profile
Получить профиль текущего пользователя

**Headers:** `Authorization: Bearer {token}`

### POST /api/auth/assign-role
Назначить роль пользователю (только Admin)

**Headers:** `Authorization: Bearer {admin_token}`

**Body:**
```json
{
  "email": "user@example.com",
  "roleName": "Manager"
}
```

---

## 🚗 Автомобили (Cars)

### GET /api/cars
Получить все автомобили (доступно всем)

### GET /api/cars/{id}
Получить автомобиль по ID (доступно всем)

### POST /api/cars
Добавить новый автомобиль (**Manager, Admin**)

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "modelId": 1,
  "sellerId": "user-id",
  "year": 2020,
  "price": 1500000,
  "mileage": 50000,
  "color": "Черный",
  "bodyType": "Седан",
  "fuelType": "Бензин",
  "transmission": "Автомат",
  "driveType": "Передний",
  "engineVolume": 2.0,
  "enginePower": 150,
  "vin": "ABC123456789",
  "description": "В отличном состоянии",
  "location": "Москва",
  "condition": "Used",
  "status": "Active"
}
```

### PUT /api/cars/{id}
Обновить автомобиль (**только Admin**)

### DELETE /api/cars/{id}
Удалить автомобиль (**только Admin**)

### GET /api/cars/active
Получить активные автомобили (представление)

---

## ⭐ Отзывы (Reviews)

### GET /api/reviews
Получить все одобренные отзывы (доступно всем)

### POST /api/reviews
Написать отзыв (**User, Manager, Admin**)

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "dealId": 1,
  "authorId": "user-id",
  "rating": 5,
  "comment": "Отличная сделка!"
}
```

---

## 👑 Админ функции (Admin)

### GET /api/admin/reviews/pending
Получить неодобренные отзывы (**только Admin**)

**Headers:** `Authorization: Bearer {admin_token}`

### PUT /api/admin/reviews/{id}/approve
Одобрить отзыв (**только Admin**)

**Headers:** `Authorization: Bearer {admin_token}`

### PUT /api/admin/reviews/{id}/reject
Отклонить отзыв (**только Admin**)

**Headers:** `Authorization: Bearer {admin_token}`

### GET /api/admin/dashboard
Панель администратора со статистикой

**Response:**
```json
{
  "totalCars": 150,
  "activeCars": 120,
  "totalDeals": 50,
  "completedDeals": 45,
  "pendingReviews": 5,
  "totalUsers": 300,
  "totalBrands": 25,
  "totalModels": 100
}
```

### GET /api/admin/users
Получить всех пользователей (**только Admin**)

### PUT /api/admin/users/{id}/toggle-active
Активировать/деактивировать пользователя (**только Admin**)

---

## 📊 Менеджер функции (Manager)

### GET /api/manager/stats/views
График просмотров

**Headers:** `Authorization: Bearer {manager_token}`

**Query params:**
- `carId` (optional): ID конкретного автомобиля
- `days` (optional, default=30): период в днях

**Response:**
```json
{
  "totalViews": 5000,
  "topViewedCars": [
    {
      "carId": 1,
      "brand": "BMW",
      "model": "X5",
      "year": 2020,
      "viewsCount": 500,
      "price": 3500000
    }
  ]
}
```

### GET /api/manager/stats/sales
График продаж

**Query params:**
- `days` (optional, default=30): период в днях

**Response:**
```json
{
  "period": "Последние 30 дней",
  "totalSales": 150000000,
  "totalCommission": 7500000,
  "completedDealsCount": 45,
  "dailySales": [
    {
      "date": "2025-11-01",
      "count": 3,
      "totalAmount": 5000000,
      "commission": 250000
    }
  ]
}
```

### GET /api/manager/stats/popular-brands
Популярные бренды

**Response:**
```json
[
  {
    "brandId": 1,
    "brandName": "BMW",
    "carsCount": 25,
    "totalViews": 5000,
    "averagePrice": 3500000
  }
]
```

### GET /api/manager/export/cars
Экспорт автомобилей в CSV (**Manager, Admin**)

**Headers:** `Authorization: Bearer {token}`

**Query params:**
- `status` (optional): фильтр по статусу

**Response:** CSV файл

### GET /api/manager/export/deals
Экспорт сделок в CSV (**Manager, Admin**)

**Query params:**
- `status` (optional): фильтр по статусу

**Response:** CSV файл

### GET /api/manager/dashboard
Панель менеджера

**Response:**
```json
{
  "totalCars": 150,
  "activeCars": 120,
  "pendingDeals": 5,
  "completedDeals": 45,
  "totalViews": 10000,
  "recentCars": [...]
}
```

---

## 🔐 Авторизация в Swagger

1. Войдите через `/api/auth/login`
2. Скопируйте токен из ответа
3. В Swagger нажмите кнопку **Authorize**
4. Введите: `Bearer {your_token}`
5. Теперь можете использовать защищенные эндпоинты!

---

## 📝 Тестовые учетные записи

После первого запуска создается админ:

- **Email:** admin@carplatform.com
- **Password:** Admin123
- **Роль:** Admin

---

## 🎯 Быстрые действия для ролей

### Админ:
- `POST /api/cars` - Добавить авто
- `POST /api/brands` - Добавить бренд
- `PUT /api/admin/reviews/{id}/approve` - Подтвердить отзыв
- `GET /api/admin/dashboard` - Статистика

### Менеджер:
- `POST /api/cars` - Добавить авто
- `GET /api/manager/stats/views` - Графики просмотров
- `GET /api/manager/stats/sales` - Графики продаж
- `GET /api/manager/export/cars` - Экспорт CSV

### Пользователь:
- `GET /api/cars` - Просмотр авто
- `POST /api/reviews` - Написать отзыв
- `POST /api/favorites` - Добавить в избранное

---

## 🚀 Пример использования

```bash
# 1. Регистрация
curl -X POST http://localhost:5263/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"Pass123","firstName":"Test","lastName":"User"}'

# 2. Логин
curl -X POST http://localhost:5263/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"Pass123"}'

# 3. Использование токена
curl -X GET http://localhost:5263/api/auth/profile \
  -H "Authorization: Bearer {your_token}"
```

---

**Готово!** 🎉 Система ролей полностью настроена и готова к работе!

