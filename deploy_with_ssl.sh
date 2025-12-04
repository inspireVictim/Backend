#!/bin/bash
# Скрипт деплоя с автоматическим созданием SSL сертификата
# Использование: ./deploy_with_ssl.sh

set -e  # Остановить при ошибке

CERT_PATH="/etc/ssl/certs/yess-cert.pfx"
CERT_PASSWORD="YesSGo!@#!"
IP_ADDRESS="5.59.232.211"

echo "🚀 Начало деплоя Yess Backend с SSL..."

# 1. Создание сертификата (если его ещё нет)
if [ ! -f "$CERT_PATH" ]; then
    echo "📝 Создание SSL сертификата..."
    
    # Создаём директории
    sudo mkdir -p /etc/ssl/certs /etc/ssl/private
    
    # Создаём сертификат и ключ
    sudo openssl req -x509 -newkey rsa:4096 \
        -keyout /etc/ssl/private/yess-cert-key.pem \
        -out /etc/ssl/certs/yess-cert.pem \
        -days 365 -nodes \
        -subj "/CN=$IP_ADDRESS/O=Yess Loyalty/C=KG" \
        -addext "subjectAltName=IP:$IP_ADDRESS" 2>/dev/null
    
    # Преобразуем в PFX формат
    sudo openssl pkcs12 -export \
        -out "$CERT_PATH" \
        -inkey /etc/ssl/private/yess-cert-key.pem \
        -in /etc/ssl/certs/yess-cert.pem \
        -passout pass:"$CERT_PASSWORD" \
        -name "Yess Backend Certificate" 2>/dev/null
    
    # Устанавливаем права доступа
    sudo chmod 644 "$CERT_PATH"
    sudo chmod 600 /etc/ssl/private/yess-cert-key.pem
    
    echo "✅ Сертификат создан: $CERT_PATH"
else
    echo "✅ Сертификат уже существует: $CERT_PATH"
fi

# 2. Установка переменных окружения (если используете systemd)
echo "🔧 Настройка переменных окружения..."
export SSL_CERT_PATH="$CERT_PATH"
export SSL_CERT_PASSWORD="$CERT_PASSWORD"

# 3. Открытие порта в firewall
echo "🔥 Настройка firewall..."
sudo ufw allow 8443/tcp || true

# 4. Здесь добавьте свои команды для деплоя
# Например:
# echo "📦 Сборка проекта..."
# dotnet build -c Release

# echo "📤 Деплой..."
# sudo systemctl restart yess-backend

# 5. Проверка
echo "🔍 Проверка..."
sleep 2

if sudo netstat -tlnp | grep -q 8443; then
    echo "✅ Порт 8443 слушается"
else
    echo "⚠️  Порт 8443 не слушается. Проверьте логи приложения."
fi

echo ""
echo "✅ Деплой завершён!"
echo ""
echo "📋 Проверьте подключение:"
echo "   curl -vk https://$IP_ADDRESS:8443/health"
echo ""
echo "🔐 Переменные окружения для вашего service:"
echo "   SSL_CERT_PATH=$CERT_PATH"
echo "   SSL_CERT_PASSWORD=$CERT_PASSWORD"

