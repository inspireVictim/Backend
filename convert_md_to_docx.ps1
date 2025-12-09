# PowerShell скрипт для конвертации MD в DOCX и создания ZIP
# Использует pandoc, если установлен, или создает простой DOCX

$mdFile = "BACKEND_DEVELOPER_KNOWLEDGE.md"
$docxFile = "BACKEND_DEVELOPER_KNOWLEDGE.docx"
$zipFile = "BACKEND_DEVELOPER_KNOWLEDGE.zip"

if (-not (Test-Path $mdFile)) {
    Write-Host "❌ Ошибка: файл $mdFile не найден!" -ForegroundColor Red
    exit 1
}

# Проверяем наличие pandoc
$pandocPath = Get-Command pandoc -ErrorAction SilentlyContinue

if ($pandocPath) {
    Write-Host "🔄 Используется pandoc для конвертации..." -ForegroundColor Yellow
    pandoc $mdFile -o $docxFile --from markdown --to docx
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ DOCX файл создан: $docxFile" -ForegroundColor Green
    } else {
        Write-Host "❌ Ошибка при конвертации через pandoc" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠️  Pandoc не установлен. Используется альтернативный метод..." -ForegroundColor Yellow
    
    # Попробуем использовать Python с python-docx
    $pythonPath = Get-Command python -ErrorAction SilentlyContinue
    if ($pythonPath) {
        Write-Host "🔄 Попытка установить python-docx..." -ForegroundColor Yellow
        python -m pip install python-docx --quiet 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "🔄 Конвертация через Python..." -ForegroundColor Yellow
            python convert_to_docx.py
        } else {
            Write-Host "❌ Не удалось установить python-docx" -ForegroundColor Red
            Write-Host "💡 Рекомендация: установите pandoc или python-docx вручную" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "❌ Python не найден" -ForegroundColor Red
        Write-Host "💡 Рекомендация: установите pandoc (https://pandoc.org/installing.html)" -ForegroundColor Yellow
        exit 1
    }
}

# Создаем ZIP архив
if (Test-Path $docxFile) {
    Write-Host "🔄 Создание ZIP архива..." -ForegroundColor Yellow
    Compress-Archive -Path $docxFile -DestinationPath $zipFile -Force
    Write-Host "✓ ZIP архив создан: $zipFile" -ForegroundColor Green
    
    Write-Host "`n✅ Конвертация завершена успешно!" -ForegroundColor Green
    Write-Host "📄 DOCX файл: $(Resolve-Path $docxFile)" -ForegroundColor Cyan
    Write-Host "📦 ZIP архив: $(Resolve-Path $zipFile)" -ForegroundColor Cyan
} else {
    Write-Host "❌ DOCX файл не был создан" -ForegroundColor Red
    exit 1
}

