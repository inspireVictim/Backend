using Microsoft.Extensions.DependencyInjection;
using YessBackend.Application.Services; // <- подключаем пространство имён с IWebhookService

namespace YessBackend.Application.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Регистрация AutoMapper
            services.AddAutoMapper(typeof(ServiceExtensions));

            // Регистрация FluentValidation
            // TODO: services.AddValidatorsFromAssemblyContaining<ServiceExtensions>();

            // ✅ Регистрация сервисов приложения
            services.AddScoped<IWebhookService, WebhookService>();

            return services;
        }
    }
}
