# 🔧 Решение проблем с HTTPS в Docker

## Проблема: Connection refused на порту 8443

### Причина 1: Docker Desktop не запущен

**Симптомы:**
```
error during connect: Get "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/...": 
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified
```

**Решение:**
1. Запустите **Docker Desktop** на Windows
2. Дождитесь полной загрузки (иконка в трее должна быть зелёной)
3. Проверьте: `docker ps`

### Причина 2: Сертификат не создан

**Симптомы:**
- Файл `./certs/yess-cert.pfx` отсутствует
- В логах: "Certificate not found"

**Решение:**

#### Вариант A: Создание на сервере (рекомендуется)

Если Docker запускается на Linux сервере, создайте сертификат там:

```bash
# На сервере
cd ~/Backend  # или где находится проект
mkdir -p certs

openssl req -x509 -newkey rsa:4096 \
    -keyout certs/yess-cert-key.pem \
    -out certs/yess-cert.pem \
    -days 365 -nodes \
    -subj "/CN=5.59.232.211/O=Yess Loyalty/C=KG" \
    -addext "subjectAltName=IP:5.59.232.211"

openssl pkcs12 -export \
    -out certs/yess-cert.pfx \
    -inkey certs/yess-cert-key.pem \
    -in certs/yess-cert.pem \
    -passout pass:"YesSGo!@#!" \
    -name "Yess Backend Certificate"
```

#### Вариант B: Установка OpenSSL на Windows

1. Скачайте OpenSSL для Windows: https://slproweb.com/products/Win32OpenSSL.html
2. Или установите через Chocolatey:
   ```powershell
   choco install openssl
   ```
3. Затем запустите скрипт создания сертификата

#### Вариант C: Использование Git Bash (если установлен Git)

```bash
# В Git Bash
cd /e/YessProjectCsharp/yess-backend-dotnet
mkdir -p certs
cd certs

openssl req -x509 -newkey rsa:4096 \
    -keyout yess-cert-key.pem \
    -out yess-cert.pem \
    -days 365 -nodes \
    -subj "/CN=5.59.232.211/O=Yess Loyalty/C=KG" \
    -addext "subjectAltName=IP:5.59.232.211"

openssl pkcs12 -export \
    -out yess-cert.pfx \
    -inkey yess-cert-key.pem \
    -in yess-cert.pem \
    -passout pass:"YesSGo!@#!" \
    -name "Yess Backend Certificate"
```

### Причина 3: Контейнеры не запущены

**Решение:**

```bash
# Проверьте статус
docker-compose ps

# Если контейнеры не запущены
docker-compose up -d

# Проверьте логи
docker-compose logs csharp-backend
```

### Причина 4: Контейнеры не перезапущены после изменений

**Решение:**

```bash
# Остановите контейнеры
docker-compose down

# Запустите заново
docker-compose up -d

# Проверьте логи
docker-compose logs -f csharp-backend
```

## 📋 Чеклист проверки

Проверьте по порядку:

- [ ] **Docker Desktop запущен** (иконка зелёная в трее)
  ```powershell
  docker ps
  ```

- [ ] **Сертификат создан** (файл существует)
  ```powershell
  Test-Path .\certs\yess-cert.pfx
  ```

- [ ] **Переменные окружения раскомментированы** в `docker-compose.yml`
  ```yaml
  - ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/yess-cert.pfx
  - ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PASSWORD=YesSGo!@#!
  ```

- [ ] **Volume раскомментирован** в `docker-compose.yml`
  ```yaml
  - ./certs:/etc/ssl/certs:ro
  ```

- [ ] **Контейнеры запущены**
  ```powershell
  docker-compose ps
  ```

- [ ] **Порт слушается в контейнере**
  ```powershell
  docker exec csharp-backend netstat -tlnp
  # или
  docker exec csharp-backend ss -tlnp
  ```

- [ ] **Сертификат доступен в контейнере**
  ```powershell
  docker exec csharp-backend ls -la /etc/ssl/certs/yess-cert.pfx
  ```

## 🔍 Диагностика

### Проверка логов

```powershell
# Логи последних 50 строк
docker-compose logs --tail=50 csharp-backend

# Логи в реальном времени
docker-compose logs -f csharp-backend
```

Ожидаемые сообщения в логах:
- ✅ `HTTP настроен на порту 5000 для обратного прокси`
- ✅ `HTTPS настроен для Production на порту 5001 с сертификатом...`
- ✅ `HTTPS Redirection и HSTS включены`

Если видите предупреждения:
- ⚠️ `HTTPS не настроен: файл сертификата не найден` → сертификат не создан
- ⚠️ `CryptographicException` → неверный пароль или повреждённый сертификат
- ⚠️ `HTTPS недоступен, редирект отключен` → HTTPS не настроен

### Проверка портов

```powershell
# Проверка портов хоста
netstat -ano | findstr ":8443"
netstat -ano | findstr ":8000"

# Проверка портов в контейнере
docker exec csharp-backend netstat -tlnp
```

### Проверка переменных окружения в контейнере

```powershell
docker exec csharp-backend env | findstr CERT
```

Должны быть:
- `ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/yess-cert.pfx`
- `ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PASSWORD=YesSGo!@#!`

## 🚀 Быстрое решение

Если ничего не помогает, выполните всё заново:

1. **Запустите Docker Desktop**

2. **Создайте сертификат** (выберите один способ):
   - На сервере через SSH
   - Установите OpenSSL и используйте скрипт
   - Используйте Git Bash

3. **Проверьте docker-compose.yml**:
   ```yaml
   environment:
     - ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/yess-cert.pfx
     - ASPNETCORE_KESTREL__CERTIFICATES__DEFAULT__PASSWORD=YesSGo!@#!
   volumes:
     - ./certs:/etc/ssl/certs:ro
   ```

4. **Перезапустите контейнеры**:
   ```powershell
   docker-compose down
   docker-compose up -d
   ```

5. **Проверьте**:
   ```powershell
   curl http://localhost:8000/health
   curl -vk https://localhost:8443/health
   ```

