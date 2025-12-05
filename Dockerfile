# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj файлы
COPY ["YessBackend.Api/YessBackend.Api.csproj", "YessBackend.Api/"]
COPY ["YessBackend.Application/YessBackend.Application.csproj", "YessBackend.Application/"]
COPY ["YessBackend.Domain/YessBackend.Domain.csproj", "YessBackend.Domain/"]
COPY ["YessBackend.Infrastructure/YessBackend.Infrastructure.csproj", "YessBackend.Infrastructure/"]

# Восстанавливаем зависимости
RUN dotnet restore "YessBackend.Api/YessBackend.Api.csproj"

# Копируем весь код
COPY . .

# Сборка и публикация
WORKDIR "/src/YessBackend.Api"
RUN dotnet build "YessBackend.Api.csproj" -c Release -o /app/build
RUN dotnet publish "YessBackend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Установка libpq для PostgreSQL
RUN apt-get update && apt-get install -y libpq-dev && rm -rf /var/lib/apt/lists/*

# Копируем опубликованное приложение
COPY --from=build /app/publish .

# Копируем сертификаты
COPY certs /app/certs

# Папка для загрузок
RUN mkdir -p /app/uploads && chmod 777 /app/uploads

# Открываем порты
EXPOSE 5000
EXPOSE 5001

ENV ASPNETCORE_ENVIRONMENT=Production

# Запуск приложения
ENTRYPOINT ["dotnet", "YessBackend.Api.dll"]
