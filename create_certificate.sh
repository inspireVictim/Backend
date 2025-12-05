#!/bin/bash
# Скрипт для создания самоподписанного SSL сертификата для Yess Backend
# Использование: sudo ./create_certificate.sh

CERT_DIR="/etc/ssl/certs"
KEY_DIR="/etc/ssl/private"
CERT_NAME="yess-cert"
PASSWORD="YesSGo!@#!"
IP_ADDRESS="5.59.232.211"

echo "🔐 Создание SSL сертификата для Yess Backend..."
echo "IP адрес: $IP_ADDRESS"
echo "Пароль сертификата: $PASSWORD"
echo ""

# Создаём директории, если их нет
sudo mkdir -p "$CERT_DIR"
sudo mkdir -p "$KEY_DIR"

# Создаём приватный ключ и сертификат
echo "📝 Генерация приватного ключа и сертификата..."
sudo openssl req -x509 -newkey rsa:4096 \
    -keyout "$KEY_DIR/$CERT_NAME-key.pem" \
    -out "$CERT_DIR/$CERT_NAME.pem" \
    -days 365 \
    -nodes \
    -subj "/CN=$IP_ADDRESS/O=Yess Loyalty/C=KG" \
    -addext "subjectAltName=IP:$IP_ADDRESS"

# Устанавливаем правильные права доступа
sudo chmod 644 "$CERT_DIR/$CERT_NAME.pem"
sudo chmod 600 "$KEY_DIR/$CERT_NAME-key.pem"

# Преобразуем в PFX формат (для .NET)
echo "🔄 Преобразование в PFX формат..."
sudo openssl pkcs12 -export \
    -out "$CERT_DIR/$CERT_NAME.pfx" \
    -inkey "$KEY_DIR/$CERT_NAME-key.pem" \
    -in "$CERT_DIR/$CERT_NAME.pem" \
    -passout pass:"$PASSWORD" \
    -name "Yess Backend Certificate"

# Устанавливаем права доступа для PFX файла
sudo chmod 644 "$CERT_DIR/$CERT_NAME.pfx"

echo ""
echo "✅ Сертификат успешно создан!"
echo "📁 Расположение сертификата: $CERT_DIR/$CERT_NAME.pfx"
echo "🔑 Пароль: $PASSWORD"
echo ""
echo "📋 Информация о сертификате:"
sudo openssl x509 -in "$CERT_DIR/$CERT_NAME.pem" -noout -subject -dates

echo ""
echo "✅ Готово! Теперь перезапустите приложение."

