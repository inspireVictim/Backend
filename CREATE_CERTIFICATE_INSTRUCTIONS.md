# 🔐 Инструкция по созданию SSL сертификата

## Для Linux сервера (рекомендуется)

1. Скопируйте скрипт `create_certificate.sh` на сервер:
   ```bash
   scp create_certificate.sh user@5.59.232.211:/tmp/
   ```

2. На сервере выполните:
   ```bash
   sudo chmod +x /tmp/create_certificate.sh
   sudo /tmp/create_certificate.sh
   ```

3. Скрипт создаст сертификат в `/etc/ssl/certs/yess-cert.pfx`

## Для Windows (локальная разработка)

1. Откройте PowerShell от имени администратора

2. Выполните скрипт:
   ```powershell
   .\create_certificate.ps1
   ```

3. Скрипт создаст сертификат в `.\certs\yess-cert.pfx`

4. Обновите путь в `appsettings.json`:
   ```json
   "Certificates": {
     "Default": {
       "Path": ".\\certs\\yess-cert.pfx",
       "Password": "YesSGo!@#!"
     }
   }
   ```

## Ручное создание (если скрипты не работают)

### Linux:
```bash
# Создать директории
sudo mkdir -p /etc/ssl/certs /etc/ssl/private

# Создать сертификат и ключ
sudo openssl req -x509 -newkey rsa:4096 \
    -keyout /etc/ssl/private/yess-cert-key.pem \
    -out /etc/ssl/certs/yess-cert.pem \
    -days 365 -nodes \
    -subj "/CN=5.59.232.211/O=Yess Loyalty/C=KG" \
    -addext "subjectAltName=IP:5.59.232.211"

# Преобразовать в PFX
sudo openssl pkcs12 -export \
    -out /etc/ssl/certs/yess-cert.pfx \
    -inkey /etc/ssl/private/yess-cert-key.pem \
    -in /etc/ssl/certs/yess-cert.pem \
    -passout pass:"YesSGo!@#!"

# Установить права доступа
sudo chmod 644 /etc/ssl/certs/yess-cert.pfx
sudo chmod 600 /etc/ssl/private/yess-cert-key.pem
```

### Windows (через OpenSSL, если установлен):
```cmd
# Создать директорию
mkdir certs

# Создать сертификат и ключ
openssl req -x509 -newkey rsa:4096 ^
    -keyout certs\yess-cert-key.pem ^
    -out certs\yess-cert.pem ^
    -days 365 -nodes ^
    -subj "/CN=5.59.232.211/O=Yess Loyalty/C=KG" ^
    -addext "subjectAltName=IP:5.59.232.211"

# Преобразовать в PFX
openssl pkcs12 -export ^
    -out certs\yess-cert.pfx ^
    -inkey certs\yess-cert-key.pem ^
    -in certs\yess-cert.pem ^
    -passout pass:"YesSGo!@#!"
```

## После создания сертификата

1. Убедитесь, что путь к сертификату в `appsettings.json` правильный
2. Перезапустите приложение
3. Проверьте, что порт 8443 слушается:
   ```bash
   sudo netstat -tlnp | grep 8443
   # или
   sudo ss -tlnp | grep 8443
   ```

4. Проверьте HTTPS подключение:
   ```bash
   curl -vk https://5.59.232.211:8443
   ```

## Примечания

- ⚠️ Самоподписанный сертификат подходит только для тестирования
- ✅ Для production рекомендуется использовать Let's Encrypt или сертификат от удостоверяющего центра
- 🔒 Пароль сертификата: `YesSGo!@#!`
- 📁 Путь на Linux: `/etc/ssl/certs/yess-cert.pfx`
- 📁 Путь на Windows: `.\certs\yess-cert.pfx` (относительно проекта)

