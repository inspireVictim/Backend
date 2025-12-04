# PowerShell скрипт для создания самоподписанного SSL сертификата для Yess Backend (Windows)
# Использование: .\create_certificate.ps1 (запустить от администратора)

$CertName = "yess-cert"
$Password = "YesSGo!@#!"
$IPAddress = "5.59.232.211"
$CertPath = ".\certs"
$CertFilePath = "$CertPath\$CertName.pfx"

Write-Host "🔐 Создание SSL сертификата для Yess Backend..." -ForegroundColor Cyan
Write-Host "IP адрес: $IPAddress" -ForegroundColor Yellow
Write-Host "Пароль сертификата: $Password" -ForegroundColor Yellow
Write-Host ""

# Проверяем права администратора
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "❌ Ошибка: Скрипт должен быть запущен от имени администратора!" -ForegroundColor Red
    exit 1
}

# Создаём директорию для сертификатов
if (-not (Test-Path $CertPath)) {
    New-Item -ItemType Directory -Path $CertPath -Force | Out-Null
    Write-Host "📁 Создана директория: $CertPath" -ForegroundColor Green
}

# Создаём самоподписанный сертификат
Write-Host "📝 Генерация самоподписанного сертификата..." -ForegroundColor Cyan

$securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

# Создаём сертификат через .NET классы
$cert = New-SelfSignedCertificate `
    -DnsName $IPAddress `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyAlgorithm RSA `
    -KeyLength 4096 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(1) `
    -FriendlyName "Yess Backend Certificate"

Write-Host "✅ Сертификат создан в хранилище Windows" -ForegroundColor Green

# Экспортируем сертификат в PFX файл
Write-Host "💾 Экспорт сертификата в PFX формат..." -ForegroundColor Cyan

$certPathInStore = "Cert:\LocalMachine\My\$($cert.Thumbprint)"
Export-PfxCertificate `
    -Cert $certPathInStore `
    -FilePath $CertFilePath `
    -Password $securePassword | Out-Null

Write-Host "✅ Сертификат экспортирован: $CertFilePath" -ForegroundColor Green

# Информация о сертификате
Write-Host ""
Write-Host "📋 Информация о сертификате:" -ForegroundColor Cyan
Write-Host "   Subject: $($cert.Subject)"
Write-Host "   Thumbprint: $($cert.Thumbprint)"
Write-Host "   Valid Until: $($cert.NotAfter)"
Write-Host ""
Write-Host "✅ Готово! Сертификат сохранён в: $CertFilePath" -ForegroundColor Green
Write-Host "⚠️  Обновите путь в appsettings.json на: $CertFilePath" -ForegroundColor Yellow

