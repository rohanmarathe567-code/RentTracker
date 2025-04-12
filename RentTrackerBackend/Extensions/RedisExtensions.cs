using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using RentTrackerBackend.Data;

namespace RentTrackerBackend.Extensions
{
    public static class RedisExtensions
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Redis multiplexer as singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConfig = configuration.GetSection("Redis:ConnectionString").Value;
                return ConnectionMultiplexer.Connect(redisConfig);
            });

            // Register PropertyRepository with Redis support
            services.AddScoped<IPropertyRepository, PropertyRepository>();

            return services;
        }
    }
}