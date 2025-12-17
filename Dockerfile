# =========================
# Stage 1: build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["YessBackend.Api/YessBackend.Api.csproj", "YessBackend.Api/"]
COPY ["YessBackend.Application/YessBackend.Application.csproj", "YessBackend.Application/"]
COPY ["YessBackend.Domain/YessBackend.Domain.csproj", "YessBackend.Domain/"]
COPY ["YessBackend.Infrastructure/YessBackend.Infrastructure.csproj", "YessBackend.Infrastructure/"]

RUN dotnet restore "YessBackend.Api/YessBackend.Api.csproj"

COPY . .
WORKDIR /src/YessBackend.Api

RUN dotnet publish "YessBackend.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# =========================
# Stage 2: runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ✅ ТОЛЬКО системные сертификаты
RUN apt-get update && \
    apt-get install -y ca-certificates curl && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# --- App ---
COPY --from=build /app/publish .

RUN mkdir -p /app/uploads && chmod 777 /app/uploads

EXPOSE 5000

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "YessBackend.Api.dll"]
