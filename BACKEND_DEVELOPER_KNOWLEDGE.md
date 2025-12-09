# Необходимые знания для разработки YESS Backend

Этот документ описывает все темы, технологии и концепции, которые должен знать бэкенд-программист для работы с данным проектом.

## 📋 Содержание

1. [Язык программирования и платформа](#1-язык-программирования-и-платформа)
2. [Архитектура приложения](#2-архитектура-приложения)
3. [Базы данных](#3-базы-данных)
4. [Аутентификация и авторизация](#4-аутентификация-и-авторизация)
5. [API и веб-фреймворки](#5-api-и-веб-фреймворки)
6. [Платежные системы и интеграции](#6-платежные-системы-и-интеграции)
7. [Кэширование](#7-кэширование)
8. [Middleware и обработка запросов](#8-middleware-и-обработка-запросов)
9. [Фоновые задачи](#9-фоновые-задачи)
10. [Контейнеризация и деплой](#10-контейнеризация-и-деплой)
11. [Безопасность](#11-безопасность)
12. [Тестирование и документация](#12-тестирование-и-документация)
13. [Инструменты разработки](#13-инструменты-разработки)

---

## 1. Язык программирования и платформа

### 1.1 C# и .NET 8.0

**Обязательные знания:**
- Синтаксис C# (классы, интерфейсы, наследование, полиморфизм)
- Асинхронное программирование (`async/await`, `Task`, `Task<T>`)
- LINQ (Language Integrated Query)
- Generics (обобщенные типы)
- Nullable reference types
- Pattern matching
- Records и init-only properties
- Extension methods
- Attributes (атрибуты)
- Reflection (базовые знания)

**Важные концепции:**
- Dependency Injection (DI)
- Inversion of Control (IoC)
- SOLID принципы
- Обработка исключений (`try-catch-finally`, `throw`)
- IDisposable и using statements
- Memory management (GC, managed/unmanaged memory)

### 1.2 .NET Core / ASP.NET Core

**Обязательные знания:**
- ASP.NET Core Web API
- Middleware pipeline
- Configuration system (`appsettings.json`, environment variables)
- Logging (ILogger, ILoggerFactory)
- Options pattern
- Hosted Services
- Health checks

---

## 2. Архитектура приложения

### 2.1 Clean Architecture / Onion Architecture

Проект использует многослойную архитектуру:

**Слои:**
- **Domain** - бизнес-логика, сущности, доменные модели
- **Application** - интерфейсы сервисов, DTOs, маппинг
- **Infrastructure** - реализация сервисов, доступ к данным, внешние интеграции
- **API** - контроллеры, middleware, конфигурация

**Необходимые знания:**
- Разделение ответственности (Separation of Concerns)
- Dependency Inversion Principle (DIP)
- Repository Pattern (неявно через EF Core)
- Service Layer Pattern
- DTO (Data Transfer Objects) Pattern
- Unit of Work Pattern (через DbContext)

### 2.2 Паттерны проектирования

**Используемые паттерны:**
- **Dependency Injection** - внедрение зависимостей через конструктор
- **Factory Pattern** - создание объектов через DI контейнер
- **Strategy Pattern** - различные стратегии платежных систем
- **Adapter Pattern** - адаптация внешних API
- **Middleware Pattern** - обработка запросов в pipeline
- **Background Service Pattern** - фоновые задачи

---

## 3. Базы данных

### 3.1 PostgreSQL

**Обязательные знания:**
- SQL синтаксис (SELECT, INSERT, UPDATE, DELETE)
- JOIN операции (INNER, LEFT, RIGHT, FULL)
- Индексы и их оптимизация
- Транзакции (ACID свойства)
- Ограничения (constraints): PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK
- JSON/JSONB типы данных
- Агрегатные функции
- Подзапросы и CTE (Common Table Expressions)

### 3.2 Entity Framework Core 9.0

**Обязательные знания:**
- Code First подход
- DbContext и DbSet
- Миграции (migrations)
- Fluent API конфигурация
- LINQ to Entities
- Eager Loading, Lazy Loading, Explicit Loading
- Change Tracking
- Raw SQL queries
- Stored Procedures (если используются)

**Важные концепции:**
- Навигационные свойства (Navigation Properties)
- Конфигурация связей (One-to-Many, Many-to-Many, One-to-One)
- Каскадное удаление (Cascade Delete)
- Индексы через Fluent API
- Value Converters
- Owned Entity Types

**Примеры из проекта:**
```csharp
// Конфигурация сущности
modelBuilder.ApplyConfiguration(new UserConfiguration());

// Работа с данными
var user = await _context.Users
    .Include(u => u.Wallet)
    .FirstOrDefaultAsync(u => u.Id == userId);
```

---

## 4. Аутентификация и авторизация

### 4.1 JWT (JSON Web Tokens)

**Обязательные знания:**
- Структура JWT (Header, Payload, Signature)
- Создание и валидация токенов
- Access Token и Refresh Token
- Claims (утверждения) в токене
- Token expiration и refresh механизм

**Используемые библиотеки:**
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`

**Примеры из проекта:**
```csharp
// Настройка JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });
```

### 4.2 Авторизация

**Обязательные знания:**
- Policy-based authorization
- Role-based authorization
- Claims-based authorization
- `[Authorize]` атрибут
- Custom authorization handlers

### 4.3 Хеширование паролей

**Используется:**
- BCrypt.Net-Next для хеширования паролей
- Salt и pepper концепции

---

## 5. API и веб-фреймворки

### 5.1 RESTful API

**Обязательные знания:**
- HTTP методы (GET, POST, PUT, PATCH, DELETE)
- HTTP статус коды (200, 201, 400, 401, 403, 404, 500)
- REST принципы
- API versioning (`/api/v1/`)
- Query parameters и route parameters
- Request/Response модели

### 5.2 ASP.NET Core Controllers

**Обязательные знания:**
- ControllerBase и Controller
- Action methods
- Model binding (`[FromBody]`, `[FromQuery]`, `[FromRoute]`)
- Action results (Ok, BadRequest, NotFound, etc.)
- Response types и ProducesResponseType
- Swagger/OpenAPI аннотации

**Примеры из проекта:**
```csharp
[HttpPost("webhook")]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult> Webhook() { }
```

### 5.3 CORS (Cross-Origin Resource Sharing)

**Обязательные знания:**
- Настройка CORS политик
- Origins, Methods, Headers
- Credentials handling

### 5.4 Swagger/OpenAPI

**Используется:**
- Swashbuckle.AspNetCore для генерации документации
- Swagger UI для интерактивной документации
- XML комментарии для описания API

---

## 6. Платежные системы и интеграции

### 6.1 Интеграция с внешними API

**Обязательные знания:**
- HttpClient и его использование
- HTTP запросы (GET, POST, PUT, DELETE)
- JSON сериализация/десериализация
- Обработка ошибок HTTP
- Retry policies
- Timeout handling

**Используемые библиотеки:**
- `System.Net.Http.Json` для JSON операций
- IHttpClientFactory для управления HttpClient

**Примеры из проекта:**
```csharp
// Регистрация HttpClient
builder.Services.AddHttpClient<IFinikPaymentService, FinikPaymentService>();

// Использование в сервисе
var response = await _httpClient.PostAsJsonAsync("/api/v1/payments", body);
```

### 6.2 Платежные системы

**Интегрированные системы:**
- **Finik Payments** - платежная система Кыргызстана
- **Optima Bank** - банковская интеграция

**Необходимые знания:**
- Webhook обработка
- RSA подписи для верификации webhook
- SHA256 хеширование
- Base64 кодирование/декодирование
- Идемпотентность платежей
- Обработка статусов платежей (SUCCEEDED, FAILED, PENDING)

**Примеры из проекта:**
```csharp
// Проверка RSA подписи
var rsaDeformatter = new RSAPKCS1SignatureDeformatter(_rsaPublicKey);
rsaDeformatter.SetHashAlgorithm("SHA256");
var isValid = rsaDeformatter.VerifySignature(dataHash, signatureBytes);
```

### 6.3 Обработка платежей

**Концепции:**
- Создание платежных транзакций
- Отслеживание статусов
- Обработка callback/webhook
- Реестры сверки (reconciliation)
- Возвраты (refunds)

---

## 7. Кэширование

### 7.1 Redis

**Обязательные знания:**
- Redis как in-memory data store
- Ключ-значение структура
- TTL (Time To Live)
- Distributed caching

**Используемые библиотеки:**
- `StackExchange.Redis`
- `Microsoft.Extensions.Caching.StackExchangeRedis`

**Примеры из проекта:**
```csharp
// Настройка Redis
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "localhost:6379";
    options.InstanceName = "YessBackend:";
});

// Использование
await _cache.SetStringAsync(key, value, options);
var value = await _cache.GetStringAsync(key);
```

### 7.2 Применение кэширования

**Используется для:**
- Rate limiting (ограничение частоты запросов)
- Кэширование часто запрашиваемых данных
- Сессии пользователей (если необходимо)

---

## 8. Middleware и обработка запросов

### 8.1 Custom Middleware

**Обязательные знания:**
- Middleware pipeline
- RequestDelegate
- Order of middleware execution
- Request/Response manipulation

**Реализованные middleware:**
- **GlobalExceptionHandlerMiddleware** - глобальная обработка исключений
- **RateLimitingMiddleware** - ограничение частоты запросов

**Примеры из проекта:**
```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try {
            await _next(context);
        } catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 8.2 Обработка исключений

**Необходимые знания:**
- Типы исключений (Exception, InvalidOperationException, etc.)
- Логирование исключений
- Возврат корректных HTTP статус кодов
- Пользовательские сообщения об ошибках

---

## 9. Фоновые задачи

### 9.1 Background Services

**Обязательные знания:**
- IHostedService интерфейс
- BackgroundService базовый класс
- CancellationToken для остановки
- Dependency Injection в фоновых сервисах
- Создание scope для доступа к scoped сервисам

**Примеры из проекта:**
```csharp
public class ReconciliationBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Выполнение задачи
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

**Реализованные фоновые задачи:**
- **ReconciliationBackgroundService** - ежедневная генерация реестров сверки

---

## 10. Контейнеризация и деплой

### 10.1 Docker

**Обязательные знания:**
- Dockerfile структура
- Multi-stage builds
- Docker images и containers
- Docker volumes
- Docker networks

**Примеры из проекта:**
```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# ... build steps ...
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# ... runtime steps ...
```

### 10.2 Docker Compose

**Обязательные знания:**
- docker-compose.yml структура
- Services, networks, volumes
- Environment variables
- Health checks
- Dependencies между сервисами

**Сервисы в проекте:**
- PostgreSQL
- Redis
- Nginx (reverse proxy)
- Backend application

### 10.3 Nginx

**Базовые знания:**
- Reverse proxy настройка
- SSL/TLS терминация
- Load balancing (если используется)
- Static file serving

### 10.4 HTTPS/SSL

**Обязательные знания:**
- SSL сертификаты
- Let's Encrypt
- Certbot
- Kestrel HTTPS настройка

---

## 11. Безопасность

### 11.1 Криптография

**Обязательные знания:**
- RSA шифрование
- SHA256 хеширование
- Base64 кодирование
- PKCS1 padding для RSA
- Публичные и приватные ключи

**Используется для:**
- Проверка подписей webhook от платежных систем
- Хеширование паролей

### 11.2 Защита API

**Меры безопасности:**
- JWT аутентификация
- HTTPS обязателен в production
- CORS настройка
- Rate limiting
- Валидация входных данных
- SQL injection защита (через параметризованные запросы EF Core)

### 11.3 Валидация данных

**Используется:**
- FluentValidation (частично)
- Data Annotations
- Model validation в ASP.NET Core

---

## 12. Тестирование и документация

### 12.1 Документация API

**Используется:**
- Swagger/OpenAPI
- XML комментарии в коде
- Postman коллекции

### 12.2 Логирование

**Обязательные знания:**
- ILogger, ILogger<T>
- Log levels (Trace, Debug, Information, Warning, Error, Critical)
- Structured logging
- Логирование в production

**Примеры из проекта:**
```csharp
_logger.LogInformation("Processing payment: {PaymentId}", paymentId);
_logger.LogError(ex, "Error processing payment");
```

---

## 13. Инструменты разработки

### 13.1 IDE и редакторы

**Рекомендуется:**
- Visual Studio 2022
- Visual Studio Code с C# расширениями
- JetBrains Rider

### 13.2 Управление версиями

**Обязательные знания:**
- Git (базовые команды)
- Git workflow (branching, merging, pull requests)
- .gitignore файлы

### 13.3 База данных инструменты

**Рекомендуется:**
- pgAdmin для PostgreSQL
- DBeaver
- Azure Data Studio

### 13.4 API тестирование

**Инструменты:**
- Postman
- Swagger UI
- curl
- HTTP файлы (.http)

### 13.5 Миграции БД

**Команды:**
```bash
# Создание миграции
dotnet ef migrations add MigrationName

# Применение миграций
dotnet ef database update

# Откат миграции
dotnet ef database update PreviousMigrationName
```

---

## 📚 Дополнительные ресурсы для изучения

### Официальная документация
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Книги
- "Clean Architecture" by Robert C. Martin
- "Designing Data-Intensive Applications" by Martin Kleppmann
- "Building Microservices" by Sam Newman

### Онлайн курсы
- Microsoft Learn - ASP.NET Core
- Pluralsight - C# и .NET курсы
- Udemy - .NET Core курсы

---

## 🎯 Чеклист для начинающего разработчика

### Базовый уровень
- [ ] Понимание C# синтаксиса и основных концепций
- [ ] Базовое понимание HTTP и REST
- [ ] Работа с Git
- [ ] Понимание SQL и реляционных БД

### Средний уровень
- [ ] ASP.NET Core Web API разработка
- [ ] Entity Framework Core
- [ ] JWT аутентификация
- [ ] Dependency Injection
- [ ] Middleware разработка

### Продвинутый уровень
- [ ] Clean Architecture
- [ ] Интеграция с внешними API
- [ ] Работа с платежными системами
- [ ] Docker и контейнеризация
- [ ] Фоновые задачи
- [ ] Криптография и безопасность

---

## 💡 Практические советы

1. **Изучайте код проекта** - лучший способ понять архитектуру
2. **Читайте документацию** - особенно для EF Core и ASP.NET Core
3. **Пишите чистый код** - следуйте SOLID принципам
4. **Логируйте все важные операции** - это поможет в отладке
5. **Тестируйте API через Swagger** - перед написанием фронтенда
6. **Используйте миграции** - не изменяйте БД вручную
7. **Изучайте ошибки** - читайте stack traces внимательно
8. **Используйте Git правильно** - делайте коммиты часто с понятными сообщениями

---

## 📝 Заключение

Данный бэкенд использует современный стек технологий .NET 8 и требует глубокого понимания как базовых, так и продвинутых концепций разработки. Постоянное обучение и практика - ключ к успешной работе с проектом.

**Приоритеты изучения:**
1. C# и .NET основы
2. ASP.NET Core Web API
3. Entity Framework Core
4. PostgreSQL
5. JWT аутентификация
6. Архитектурные паттерны
7. Интеграции и платежные системы
8. Docker и деплой

Удачи в изучении! 🚀

