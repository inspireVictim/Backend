#!/bin/bash
# Скрипт запуска бэкенда и панелей для Linux/Mac
# Использование: ./start-panels.sh

set -e

echo "🚀 Запуск YESS Backend и Admin/Partner Panels..."

# Проверка Docker
echo ""
echo "📦 Проверка Docker..."
if ! command -v docker &> /dev/null; then
    echo "❌ Docker не установлен или не в PATH"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose не установлен или не в PATH"
    exit 1
fi

# Остановка существующих контейнеров
echo ""
echo "🛑 Остановка существующих контейнеров..."
docker-compose -f docker-compose.yml -f docker-compose.panels.yml down 2>/dev/null || true

# Запуск базовых сервисов
echo ""
echo "🐳 Запуск базовых сервисов (PostgreSQL, Redis, C# Backend)..."
docker-compose up -d postgres redis csharp-backend

# Ждем готовности бэкенда
echo ""
echo "⏳ Ожидание готовности бэкенда..."
MAX_ATTEMPTS=30
ATTEMPT=0
BACKEND_READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ] && [ "$BACKEND_READY" = false ]; do
    sleep 2
    ATTEMPT=$((ATTEMPT + 1))
    
    if curl -f -s http://localhost:8000/api/v1/health > /dev/null 2>&1; then
        BACKEND_READY=true
        echo "✅ Бэкенд готов!"
    else
        echo -n "."
    fi
done

if [ "$BACKEND_READY" = false ]; then
    echo ""
    echo "⚠️ Бэкенд не отвечает, но продолжаем запуск панелей..."
fi

# Запуск панелей
echo ""
echo "🎨 Запуск Admin и Partner панелей..."
docker-compose -f docker-compose.yml -f docker-compose.panels.yml up -d admin-panel partner-panel nginx-proxy

# Ожидание готовности панелей
echo ""
echo "⏳ Ожидание готовности панелей..."
sleep 10

# Проверка статуса
echo ""
echo "📊 Статус сервисов:"
docker-compose -f docker-compose.yml -f docker-compose.panels.yml ps

echo ""
echo "✅ Запуск завершен!"
echo ""
echo "📍 Доступные сервисы:"
echo "  🌐 API Backend:     http://localhost:8000"
echo "  📚 Swagger:         http://localhost:8000/docs"
echo "  👨‍💼 Admin Panel:     http://localhost:3003 или http://localhost/admin"
echo "  🤝 Partner Panel:   http://localhost:3004 или http://localhost/partner"
echo "  🔄 Nginx Proxy:     http://localhost"
echo ""
echo "💡 Используйте 'docker-compose -f docker-compose.yml -f docker-compose.panels.yml logs -f' для просмотра логов"

