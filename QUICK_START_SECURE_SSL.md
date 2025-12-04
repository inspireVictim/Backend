# ⚡ Быстрый старт: Безопасная настройка SSL

## 📋 Краткая инструкция

### Шаг 1: На сервере создайте сертификат

```bash
# Вариант A: Используя готовый скрипт
sudo chmod +x create_certificate.sh
sudo ./create_certificate.sh

# Вариант B: Используя скрипт деплоя (автоматически создаст, если нет)
sudo chmod +x deploy_with_ssl.sh
sudo ./deploy_with_ssl.sh
```

### Шаг 2: Настройте переменные окружения

**Для systemd service:**

Создайте/отредактируйте `/etc/systemd/system/yess-backend.service`:

```ini
[Service]
Environment=SSL_CERT_PATH=/etc/ssl/certs/yess-cert.pfx
Environment=SSL_CERT_PASSWORD=YesSGo!@#!
```

Затем:
```bash
sudo systemctl daemon-reload
sudo systemctl restart yess-backend
```

**Для Docker:**

Добавьте в `docker-compose.yml`:
```yaml
environment:
  - SSL_CERT_PATH=/etc/ssl/certs/yess-cert.pfx
  - SSL_CERT_PASSWORD=YesSGo!@#!
volumes:
  - /etc/ssl/certs:/etc/ssl/certs:ro
```

**Для ручного запуска:**

```bash
export SSL_CERT_PATH=/etc/ssl/certs/yess-cert.pfx
export SSL_CERT_PASSWORD=YesSGo!@#!
dotnet run
```

### Шаг 3: Проверьте

```bash
# Откройте firewall (если ещё не открыт)
sudo ufw allow 8443/tcp

# Проверьте, что порт слушается
sudo netstat -tlnp | grep 8443

# Проверьте HTTPS
curl -vk https://5.59.232.211:8443/health
```

## ✅ Готово!

Подробная инструкция: см. `SECURE_SSL_SETUP.md`

## 🔐 Безопасность

- ✅ Пароль **НЕ** хранится в `appsettings.json`
- ✅ Сертификаты **НЕ** попадают в git (через `.gitignore`)
- ✅ Используются переменные окружения для секретов

## 📝 Что было изменено

1. ✅ `Program.cs` - поддержка переменных окружения `SSL_CERT_PATH` и `SSL_CERT_PASSWORD`
2. ✅ `appsettings.json` - убран пароль (остался только путь)
3. ✅ `.gitignore` - добавлены правила для исключения сертификатов
4. ✅ Создан шаблон `appsettings.Production.json.example`

