using YessBackend.Application.Config;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using YessBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using YessBackend.Application.Extensions;
using YessBackend.Infrastructure.Extensions;
using YessBackend.Application.Services;
using YessBackend.Infrastructure.Services;
using YessBackend.Api.Middleware;
using YessBackend.Application.Interfaces.Payments;

var builder = WebApplication.CreateBuilder(args);

// =======================
//   Kestrel HTTP/HTTPS
// =======================
// Флаг для отслеживания успешной настройки HTTPS endpoint
bool httpsAvailable = false;

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP всегда включён на порту 5000 (для обратного прокси nginx)
    options.ListenAnyIP(5000);
    
    if (builder.Environment.IsDevelopment())
    {
        // Development: автоматически использует dev-сертификат
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.UseHttps();
        });
        httpsAvailable = true;
    }
    else
    {
        // Production: загрузка сертификата из конфигурации
        var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
        var certPassword = builder.Configuration["Kestrel:Certificates:Default:Password"];
        
        if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath))
        {
            try
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    if (string.IsNullOrWhiteSpace(certPassword))
                        listenOptions.UseHttps(certPath);
                    else
                        listenOptions.UseHttps(certPath, certPassword);
                });
                httpsAvailable = true;
            }
            catch (CryptographicException)
            {
                // Логирование будет выполнено после создания logger
                // Приложение продолжит работу без HTTPS
            }
            catch (Exception)
            {
                // Обработка других исключений при загрузке сертификата
                // Приложение продолжит работу без HTTPS
            }
        }
    }
});

var configuration = builder.Configuration;

// =======================
//     Finik Payment
// =======================
builder.Services.Configure<FinikPaymentConfig>(
    configuration.GetSection("FinikPayment"));

// ВАЖНО: Добавляем сервис подписи обратно
builder.Services.AddScoped<YessBackend.Application.Interfaces.Payments.IFinikSignatureService, FinikSignatureService>();

// HttpClient для Finik
builder.Services.AddHttpClient<IFinikPaymentService, FinikPaymentService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

// =======================
//       Controllers
// =======================
// ❗ Убираем CamelCase → Finik требует строгие имена
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

// =======================
//          CORS
// =======================
var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();

        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                origin.Contains("localhost") || origin.Contains("127.0.0.1"));
        }
    });
});

// =======================
//          JWT
// =======================
var jwtSettings = configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey не настроен");

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// =======================
//        Swagger
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
//    PostgreSQL EF Core
// =======================
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' не найден");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// =======================
//         Redis
// =======================
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "YessBackend:";
});

// =======================
//   Base services
// =======================
builder.Services.AddHttpClient();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(configuration);
builder.Services.AddYessBackendServices();

// Background worker
builder.Services.AddHostedService<YessBackend.Infrastructure.Services.ReconciliationBackgroundService>();

var app = builder.Build();

// =======================
//   AUTO APPLY MIGRATIONS
// =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =======================
//   HTTPS Redirection
// =======================
var httpsLogger = app.Services.GetRequiredService<ILogger<Program>>();

if (httpsAvailable)
{
    app.UseHttpsRedirection();
    
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        httpsLogger.LogInformation("HTTPS Redirection и HSTS включены");
    }
    else
    {
        httpsLogger.LogInformation("HTTPS Redirection включён для Development");
    }
}
else
{
    httpsLogger.LogWarning("HTTPS недоступен, редирект отключен");
    
    // Логируем причину для Production
    if (!app.Environment.IsDevelopment())
    {
        var certPath = configuration["Kestrel:Certificates:Default:Path"];
        if (string.IsNullOrWhiteSpace(certPath))
        {
            httpsLogger.LogWarning("Путь к сертификату не указан в конфигурации");
        }
        else if (!File.Exists(certPath))
        {
            httpsLogger.LogWarning($"Сертификат не найден по пути: {certPath}");
        }
    }
}

// =======================
//        Swagger UI
// =======================
if (app.Environment.IsDevelopment() || configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "YESS API v1");
        c.RoutePrefix = "docs";
    });
}

// =======================
//  Middleware pipeline
// =======================
app.UseGlobalExceptionHandler();
app.UseRateLimiting(configuration);
app.UseCors("AllowCors");
app.UseAuthentication();
app.UseAuthorization();

// =======================
//        Endpoints
// =======================
app.MapGet("/", () => new
{
    status = "ok",
    service = "yess-backend",
    api = "/api/v1",
    docs = "/docs"
});

app.MapGet("/health", () => new
{
    status = "healthy",
    service = "yess-backend",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
});

app.MapControllers();
app.Run();
