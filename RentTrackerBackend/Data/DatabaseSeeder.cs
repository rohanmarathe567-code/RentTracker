using RentTrackerBackend.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace RentTrackerBackend.Data;

public class DatabaseSeeder
{
    private readonly IMongoDatabase _database;
    private readonly string _systemTenantId = "system";

    public DatabaseSeeder(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public async Task SeedAsync()
    {
        // Only seed if the database is empty
        var paymentMethodsCollection = _database.GetCollection<PaymentMethod>(nameof(PaymentMethod));
        if (await paymentMethodsCollection.CountDocumentsAsync(FilterDefinition<PaymentMethod>.Empty) > 0)
        {
            return;
        }

        // Create payment methods
        var paymentMethods = new[]
        {
            new PaymentMethod
            {
                TenantId = _systemTenantId,
                Name = "Bank Transfer",
                Description = "Direct bank transfer payment",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                TenantId = _systemTenantId,
                Name = "Credit Card",
                Description = "Payment via credit card",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                TenantId = _systemTenantId,
                Name = "Cash",
                Description = "Cash payment",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                TenantId = _systemTenantId,
                Name = "PayPal",
                Description = "Payment through PayPal service",
                IsSystemDefault = true
            }
        };

        await paymentMethodsCollection.InsertManyAsync(paymentMethods);
        var bankTransfer = paymentMethods[0]; // Keep reference for sample payments

        // Create sample rental properties
        var propertiesCollection = _database.GetCollection<RentalProperty>(nameof(RentalProperty));
        var property1 = new RentalProperty
        {
            TenantId = _systemTenantId,
            Address = new Address
            {
                Street = "123 Main Street",
                City = "Sydney",
                State = "NSW",
                ZipCode = "2000"
            },
            RentAmount = 650.00M,
            LeaseDates = new LeaseDates
            {
                StartDate = DateTime.UtcNow.AddMonths(-6),
                EndDate = DateTime.UtcNow.AddMonths(6)
            },
            Attributes = new Dictionary<string, object>
            {
                { "PropertyManager", "John Smith" },
                { "PropertyManagerContact", "john.smith@realestate.com" },
                { "Description", "Modern 2 bedroom apartment in the heart of Sydney" }
            }
        };

        var property2 = new RentalProperty
        {
            TenantId = _systemTenantId,
            Address = new Address
            {
                Street = "45 Beach Road",
                City = "Bondi",
                State = "NSW",
                ZipCode = "2026"
            },
            RentAmount = 950.00M,
            LeaseDates = new LeaseDates
            {
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(10)
            },
            Attributes = new Dictionary<string, object>
            {
                { "PropertyManager", "Sarah Johnson" },
                { "PropertyManagerContact", "sarah.j@realestate.com" },
                { "Description", "Spacious 3 bedroom house with ocean views" }
            }
        };

        await propertiesCollection.InsertManyAsync(new[] { property1, property2 });

        // Create sample rental payments
        var paymentsCollection = _database.GetCollection<RentalPayment>(nameof(RentalPayment));
        var payments = new[]
        {
            new RentalPayment
            {
                TenantId = _systemTenantId,
                RentalPropertyId = property1.Id.ToString(),
                Amount = 2600.00M, // 4 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-5),
                PaymentMethodId = bankTransfer.Id.ToString(),
                PaymentReference = "RENT-123456",
                Notes = "Initial payment including bond"
            },
            new RentalPayment
            {
                TenantId = _systemTenantId,
                RentalPropertyId = property1.Id.ToString(),
                Amount = 1300.00M, // 2 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-4),
                PaymentMethodId = bankTransfer.Id.ToString(),
                PaymentReference = "RENT-123457"
            },
            new RentalPayment
            {
                TenantId = _systemTenantId,
                RentalPropertyId = property2.Id.ToString(),
                Amount = 3800.00M, // 4 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-2),
                PaymentMethodId = bankTransfer.Id.ToString(),
                PaymentReference = "RENT-123458",
                Notes = "Initial payment including bond"
            }
        };

        await paymentsCollection.InsertManyAsync(payments);

        // Create sample attachments
        var attachmentsCollection = _database.GetCollection<Attachment>(nameof(Attachment));
        var attachments = new[]
        {
            new Attachment
            {
                TenantId = _systemTenantId,
                FileName = "lease_agreement.pdf",
                ContentType = "application/pdf",
                StoragePath = "/storage/property/lease_agreement.pdf",
                FileSize = 1024 * 1024, // 1MB
                Description = "Signed lease agreement",
                EntityType = "Property",
                RentalPropertyId = property1.Id.ToString(),
                Tags = new[] { "lease", "agreement", "signed" }
            },
            new Attachment
            {
                TenantId = _systemTenantId,
                FileName = "payment_receipt.pdf",
                ContentType = "application/pdf",
                StoragePath = "/storage/payment/receipt.pdf",
                FileSize = 512 * 1024, // 512KB
                Description = "Payment receipt",
                EntityType = "Payment",
                RentalPaymentId = payments[0].Id.ToString(),
                Tags = new[] { "receipt", "payment" }
            }
        };

        await attachmentsCollection.InsertManyAsync(attachments);

        // Update properties with payment and attachment references
        var updateProperty1 = Builders<RentalProperty>.Update
            .Push(p => p.PaymentIds, payments[0].Id.ToString())
            .Push(p => p.PaymentIds, payments[1].Id.ToString())
            .Push(p => p.AttachmentIds, attachments[0].Id.ToString());

        var updateProperty2 = Builders<RentalProperty>.Update
            .Push(p => p.PaymentIds, payments[2].Id.ToString());

        await propertiesCollection.UpdateOneAsync(
            p => p.Id == property1.Id,
            updateProperty1);

        await propertiesCollection.UpdateOneAsync(
            p => p.Id == property2.Id,
            updateProperty2);
    }
}