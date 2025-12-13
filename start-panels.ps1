# Скрипт запуска бэкенда и панелей для Windows
# Использование: .\start-panels.ps1

Write-Host "🚀 Запуск YESS Backend и Admin/Partner Panels..." -ForegroundColor Green

# Проверка Docker
Write-Host "`n📦 Проверка Docker..." -ForegroundColor Yellow
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker не установлен или не в PATH" -ForegroundColor Red
    exit 1
}

# Проверка Docker Compose
if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker Compose не установлен или не в PATH" -ForegroundColor Red
    exit 1
}

# Остановка существующих контейнеров
Write-Host "`n🛑 Остановка существующих контейнеров..." -ForegroundColor Yellow
docker-compose -f docker-compose.yml -f docker-compose.panels.yml down 2>$null

# Запуск базовых сервисов (postgres, redis, backend)
Write-Host "`n🐳 Запуск базовых сервисов (PostgreSQL, Redis, C# Backend)..." -ForegroundColor Yellow
docker-compose up -d postgres redis csharp-backend

# Ждем готовности бэкенда
Write-Host "`n⏳ Ожидание готовности бэкенда..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$backendReady = $false

while ($attempt -lt $maxAttempts -and -not $backendReady) {
    Start-Sleep -Seconds 2
    $attempt++
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8000/api/v1/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $backendReady = $true
            Write-Host "✅ Бэкенд готов!" -ForegroundColor Green
        }
    } catch {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $backendReady) {
    Write-Host "`n⚠️ Бэкенд не отвечает, но продолжаем запуск панелей..." -ForegroundColor Yellow
}

# Запуск панелей
Write-Host "`n🎨 Запуск Admin и Partner панелей..." -ForegroundColor Yellow
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d admin-panel partner-panel nginx-proxy

# Ожидание готовности панелей
Write-Host "`n⏳ Ожидание готовности панелей..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Проверка статуса
Write-Host "`n📊 Статус сервисов:" -ForegroundColor Cyan
docker-compose -f docker-compose.yml -f docker-compose.panels.yml ps

Write-Host "`n✅ Запуск завершен!" -ForegroundColor Green
Write-Host "`n📍 Доступные сервисы:" -ForegroundColor Cyan
Write-Host "  🌐 API Backend:     http://localhost:8000" -ForegroundColor White
Write-Host "  📚 Swagger:         http://localhost:8000/docs" -ForegroundColor White
Write-Host "  👨‍💼 Admin Panel:     http://localhost:3003 или http://localhost/admin" -ForegroundColor White
Write-Host "  🤝 Partner Panel:   http://localhost:3004 или http://localhost/partner" -ForegroundColor White
Write-Host "  🔄 Nginx Proxy:     http://localhost" -ForegroundColor White
Write-Host "`n💡 Используйте 'docker-compose -f docker-compose.yml -f docker-compose.panels.yml logs -f' для просмотра логов" -ForegroundColor Yellow

