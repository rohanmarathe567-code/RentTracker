using RentTrackerBackend.Models;
using MongoDB.Driver;

namespace RentTrackerBackend.Data.Seeders;

public static class PaymentMethodSeeder
{
    public static async Task SeedPaymentMethods(IMongoRepository<PaymentMethod> repository)
    {
        // Check if any system payment methods exist
        var existingMethods = await repository.GetAllAsync("system");
        if (existingMethods.Any())
            return;

        // Define default payment methods
        var defaultMethods = new List<PaymentMethod>
        {
            new PaymentMethod
            {
                Name = "Bank Transfer",
                Description = "Direct bank transfer payment",
                IsSystemDefault = true,
                TenantId = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Name = "Cash",
                Description = "Cash payment",
                IsSystemDefault = true,
                TenantId = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Name = "Check",
                Description = "Check payment",
                IsSystemDefault = true,
                TenantId = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Insert default payment methods
        foreach (var method in defaultMethods)
        {
            await repository.CreateAsync(method);
        }
    }
}