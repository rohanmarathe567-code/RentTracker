using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Extensions
{
    public static class MongoDbExtensions
    {
        public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure MongoDb Settings
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));

            // Register MongoClient as Singleton
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                return new MongoClient(settings.ConnectionString);
            });

            // Register Generic Repository
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

            // Register Specific Repositories
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Register Database Seeder
            services.AddScoped<DatabaseSeeder>();

            return services;
        }
    }
}