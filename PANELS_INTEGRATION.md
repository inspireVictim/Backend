# Интеграция Admin и Partner панелей с C# Backend

## Обзор

Этот документ описывает интеграцию TypeScript/React панелей (admin-panel и partner-panel) с C# бэкендом и настройку запуска на сервере.

## Структура

- **Admin Panel**: React + TypeScript приложение для администраторов
  - Порт в dev: 3003
  - Порт в prod: 80 (через Docker) или 3003 (прямой доступ)

- **Partner Panel**: React + TypeScript приложение для партнеров
  - Порт в dev: 3004
  - Порт в prod: 80 (через Docker) или 3004 (прямой доступ)

- **C# Backend**: ASP.NET Core API
  - Порт: 8000 (HTTP), 8443 (HTTPS)

## Быстрый старт

### Запуск всех сервисов через Docker Compose

#### Windows (PowerShell):
```powershell
.\start-panels.ps1
```

#### Linux/Mac:
```bash
chmod +x start-panels.sh
./start-panels.sh
```

#### Или вручную:
```bash
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d
```

### Разработка (локальный запуск панелей)

1. **Запустите C# Backend**:
   ```bash
   cd yess-backend-dotnet
   docker-compose up -d postgres redis
   dotnet run --project YessBackend.Api
   ```

2. **Запустите Admin Panel**:
   ```bash
   cd PANEL-s_YESS-Go/panels-ts-v2/admin-panel
   npm install
   npm run dev
   ```
   Откроется на http://localhost:3003

3. **Запустите Partner Panel**:
   ```bash
   cd PANEL-s_YESS-Go/panels-ts-v2/partner-panel
   npm install
   npm run dev
   ```
   Откроется на http://localhost:3004

## Конфигурация

### Vite Config

Панели настроены для проксирования API запросов на `http://localhost:8000` (C# Backend).

- **Development**: Vite проксирует `/api` на `http://localhost:8000`
- **Production**: Nginx проксирует `/api` на `http://csharp-backend:5000` (внутри Docker сети)

### API Base URL

Панели автоматически определяют базовый URL API:
- В development: используется относительный путь `/api/v1` (проксируется через Vite)
- В production: используется относительный путь `/api/v1` (проксируется через Nginx)

### Переменные окружения

Можно задать явный API URL через переменную окружения:
```bash
VITE_API_URL=http://your-backend-url:8000
```

## Docker Compose

Файл `docker-compose.panels.yml` добавляет следующие сервисы:

- **admin-panel**: Собранная админ-панель в nginx
- **partner-panel**: Собранная партнерская панель в nginx
- **nginx-proxy**: Обратный прокси для объединения всех сервисов

### Порты

- **80**: Nginx reverse proxy (объединяет все сервисы)
- **3003**: Admin Panel (прямой доступ)
- **3004**: Partner Panel (прямой доступ)
- **8000**: C# Backend API
- **8443**: C# Backend HTTPS

### Доступ через Nginx Proxy

После запуска доступны следующие URL:

- `http://localhost/admin` - Admin Panel
- `http://localhost/partner` - Partner Panel
- `http://localhost/api` - API Backend
- `http://localhost/docs` - Swagger документация

## Production деплой

### 1. Сборка панелей

```bash
cd PANEL-s_YESS-Go/panels-ts-v2/admin-panel
npm install
npm run build:prod

cd ../partner-panel
npm install
npm run build:prod
```

### 2. Сборка Docker образов

```bash
cd yess-backend-dotnet
docker-compose -f docker-compose.yml -f docker-compose.panels.yml build
```

### 3. Запуск

```bash
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d
```

### 4. Проверка логов

```bash
# Все логи
docker-compose -f docker-compose.yml -f docker-compose.panels.yml logs -f

# Конкретный сервис
docker-compose -f docker-compose.yml -f docker-compose.panels.yml logs -f admin-panel
```

## Обновление панелей

При изменении кода панелей:

1. Пересоберите образы:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.panels.yml build admin-panel partner-panel
   ```

2. Перезапустите контейнеры:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d admin-panel partner-panel
   ```

## Настройка CORS в C# Backend

Убедитесь, что в `Program.cs` или `appsettings.json` настроены правильные CORS origins:

```json
{
  "Cors": {
    "Origins": [
      "http://localhost:3003",
      "http://localhost:3004",
      "http://localhost",
      "http://your-domain.com"
    ]
  }
}
```

## Troubleshooting

### Панели не подключаются к API

1. Проверьте, что бэкенд запущен:
   ```bash
   curl http://localhost:8000/api/v1/health
   ```

2. Проверьте логи бэкенда:
   ```bash
   docker-compose logs csharp-backend
   ```

3. Проверьте логи панелей:
   ```bash
   docker-compose logs admin-panel
   docker-compose logs partner-panel
   ```

### Ошибки сборки панелей

1. Убедитесь, что установлены все зависимости:
   ```bash
   cd PANEL-s_YESS-Go/panels-ts-v2/admin-panel
   rm -rf node_modules package-lock.json
   npm install
   ```

2. Проверьте версию Node.js (требуется Node 18+):
   ```bash
   node --version
   ```

### Порты заняты

Измените порты в `docker-compose.panels.yml`:

```yaml
ports:
  - "3005:80"  # Вместо 3003:80 для admin-panel
```

## Структура файлов

```
yess-backend-dotnet/
├── docker-compose.yml              # Базовые сервисы (postgres, redis, backend)
├── docker-compose.panels.yml       # Панели и nginx
├── nginx-panels.conf               # Конфигурация Nginx reverse proxy
├── start-panels.ps1               # Скрипт запуска (Windows)
├── start-panels.sh                # Скрипт запуска (Linux/Mac)
└── PANELS_INTEGRATION.md          # Этот файл

PANEL-s_YESS-Go/panels-ts-v2/
├── admin-panel/
│   ├── Dockerfile
│   ├── vite.config.ts             # Настроен для проксирования на :8000
│   └── nginx.conf                 # Конфигурация nginx для статики
└── partner-panel/
    ├── Dockerfile
    ├── vite.config.ts             # Настроен для проксирования на :8000
    └── nginx.conf                 # Конфигурация nginx для статики
```

## Дополнительная информация

- [Документация панелей](../PANEL-s_YESS-Go/panels-ts-v2/README.md)
- [Архитектура панелей](../PANEL-s_YESS-Go/panels-ts-v2/docs/ARCHITECTURE.md)

