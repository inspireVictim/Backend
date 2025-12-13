# 🚀 Быстрый старт: Запуск панелей с C# Backend

## Вариант 1: Запуск через Docker Compose (Рекомендуется)

### Windows:
```powershell
# Запуск базовых сервисов
docker-compose up -d

# Запуск панелей
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d
```

Или используйте скрипт:
```powershell
.\start-panels.ps1
```

### Linux/Mac:
```bash
# Запуск базовых сервисов
docker-compose up -d

# Запуск панелей
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d
```

Или используйте скрипт:
```bash
chmod +x start-panels.sh
./start-panels.sh
```

## Вариант 2: Разработка (локальный запуск)

### 1. Запустите бэкенд:
```bash
cd yess-backend-dotnet
docker-compose up -d postgres redis
dotnet run --project YessBackend.Api
```

### 2. Запустите Admin Panel:
```bash
cd ../PANEL-s_YESS-Go/panels-ts-v2/admin-panel
npm install
npm run dev
```
Доступно на: http://localhost:3003

### 3. Запустите Partner Panel:
```bash
cd ../partner-panel
npm install
npm run dev
```
Доступно на: http://localhost:3004

## Доступные URL после запуска

После запуска через Docker Compose:

- **Admin Panel**: http://localhost:3003 или http://localhost/admin
- **Partner Panel**: http://localhost:3004 или http://localhost/partner
- **API Backend**: http://localhost:8000
- **Swagger**: http://localhost:8000/docs
- **Nginx Proxy**: http://localhost (объединяет все сервисы)

## Проверка статуса

```bash
docker-compose -f docker-compose.yml -f docker-compose.panels.yml ps
```

## Просмотр логов

```bash
# Все логи
docker-compose -f docker-compose.yml -f docker-compose.panels.yml logs -f

# Конкретный сервис
docker-compose logs -f admin-panel
docker-compose logs -f partner-panel
docker-compose logs -f csharp-backend
```

## Остановка

```bash
docker-compose -f docker-compose.yml -f docker-compose.panels.yml down
```

## Дополнительная информация

См. [PANELS_INTEGRATION.md](./PANELS_INTEGRATION.md) для детальной документации.

