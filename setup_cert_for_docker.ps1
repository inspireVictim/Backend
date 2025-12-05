# Создание SSL сертификата для Docker (Windows)
# Использование: .\setup_cert_for_docker.ps1

$CERT_DIR = ".\certs"
$CERT_NAME = "yess-cert"
$PASSWORD = "YesSGo!@#!"
$IP_ADDRESS = "5.59.232.211"
$CERT_PATH = Join-Path $CERT_DIR "$CERT_NAME.pfx"

Write-Host "Создание SSL сертификата для Docker" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Создаём директорию для сертификатов
if (-not (Test-Path $CERT_DIR)) {
    New-Item -ItemType Directory -Path $CERT_DIR | Out-Null
}

# Проверяем, существует ли сертификат
if (Test-Path $CERT_PATH) {
    Write-Host "Сертификат уже существует: $CERT_PATH" -ForegroundColor Yellow
    $response = Read-Host "Пересоздать? (y/n)"
    if ($response -ne "y" -and $response -ne "Y") {
        Write-Host "Используется существующий сертификат." -ForegroundColor Green
        exit 0
    }
    Remove-Item $CERT_PATH -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $CERT_DIR "$CERT_NAME.pem") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $CERT_DIR "$CERT_NAME-key.pem") -ErrorAction SilentlyContinue
}

Write-Host "Создание сертификата..." -ForegroundColor Cyan
Write-Host "   IP адрес: $IP_ADDRESS"
Write-Host "   Пароль: $PASSWORD"
Write-Host ""

# Создаём сертификат и ключ
$KEY_PATH = Join-Path $CERT_DIR "$CERT_NAME-key.pem"
$PEM_PATH = Join-Path $CERT_DIR "$CERT_NAME.pem"

# Проверяем наличие openssl
$openssl = Get-Command openssl -ErrorAction SilentlyContinue
if (-not $openssl) {
    Write-Host "Ошибка: openssl не найден в PATH" -ForegroundColor Red
    Write-Host "Установите OpenSSL или добавьте его в PATH" -ForegroundColor Red
    exit 1
}

# Создаём сертификат
openssl req -x509 -newkey rsa:4096 -keyout $KEY_PATH -out $PEM_PATH -days 365 -nodes -subj "/CN=$IP_ADDRESS/O=Yess Loyalty/C=KG" -addext "subjectAltName=IP:$IP_ADDRESS" | Out-Null

if (-not (Test-Path $PEM_PATH)) {
    Write-Host "Ошибка: не удалось создать сертификат" -ForegroundColor Red
    exit 1
}

# Преобразуем в PFX формат
openssl pkcs12 -export -out $CERT_PATH -inkey $KEY_PATH -in $PEM_PATH -passout "pass:$PASSWORD" -name "Yess Backend Certificate" | Out-Null

if (-not (Test-Path $CERT_PATH)) {
    Write-Host "Ошибка: не удалось создать PFX файл" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Сертификат успешно создан!" -ForegroundColor Green
Write-Host "   Расположение: $CERT_PATH" -ForegroundColor White
Write-Host "   Пароль: $PASSWORD" -ForegroundColor White
Write-Host ""
Write-Host "Готово! Теперь запустите docker-compose up -d" -ForegroundColor Green
Write-Host ""
