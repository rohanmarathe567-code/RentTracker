using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using BCrypt.Net;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Auth;

namespace RentTrackerBackend.Data;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 13);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

public class DatabaseSeeder
{
    private readonly IMongoDatabase _database;
    private readonly string _systemEmail = "admin@abc.com";
    private ObjectId _systemUserId; // Will be set after creating the admin user
    private readonly string _systemPassword = "test1234";
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(
        IMongoClient client,
        IOptions<MongoDbSettings> settings,
        IPasswordHasher passwordHasher)
    {
        _database = client.GetDatabase(settings.Value.DatabaseName);
        _passwordHasher = passwordHasher;
    }

    private async Task DropCollections()
    {
        // Drop all collections to ensure clean state
        Console.WriteLine("Dropping existing collections...");
        try
        {
            await _database.DropCollectionAsync(nameof(User));
            await _database.DropCollectionAsync(nameof(RentalProperty));
            await _database.DropCollectionAsync(nameof(PaymentMethod));
            await _database.DropCollectionAsync(nameof(RentalPayment));
            await _database.DropCollectionAsync(nameof(Attachment));
            Console.WriteLine("Collections dropped successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dropping collections: {ex.Message}");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        // Drop existing collections first to start fresh
        await DropCollections();

        // Create system user
        var usersCollection = _database.GetCollection<User>(nameof(User));
        var systemUser = new User
        {
            Email = _systemEmail,
            FirstName = "Rahul",
            MiddleName = "Anant",
            LastName = "Bedge",
            PasswordHash = _passwordHasher.HashPassword(_systemPassword),
            UserType = UserType.Admin
        };
        await usersCollection.InsertOneAsync(systemUser);
        _systemUserId = systemUser.Id; // Store the admin's Id for use as TenantId
        systemUser.TenantId = _systemUserId.ToString();
        await usersCollection.ReplaceOneAsync(x => x.Id == systemUser.Id, systemUser);

        // Get collection references for other data
        var propertiesCollection = _database.GetCollection<RentalProperty>(nameof(RentalProperty));
        var paymentMethodsCollection = _database.GetCollection<PaymentMethod>(nameof(PaymentMethod));        // Create payment methods
        var paymentMethods = new[]
        {
            new PaymentMethod
            {
                Name = "Bank Transfer",
                Description = "Direct bank transfer payment",
                IsSystemDefault = true,
                TenantId = "system"
            },
            new PaymentMethod
            {
                Name = "Credit Card",
                Description = "Payment via credit card",
                IsSystemDefault = true,
                TenantId = "system"
            },
            new PaymentMethod
            {
                Name = "Cash",
                Description = "Cash payment",
                IsSystemDefault = true,
                TenantId = "system"
            },
            new PaymentMethod
            {
                Name = "PayPal",
                Description = "Payment through PayPal service",
                IsSystemDefault = true,
                TenantId = "system"
            }
        };

        await paymentMethodsCollection.InsertManyAsync(paymentMethods);
        var bankTransfer = paymentMethods[0]; // Keep reference for sample payments

        // Create sample rental properties
        // Create sample rental properties
        var property1 = new RentalProperty
        {
            TenantId = _systemUserId.ToString(),
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
            Description = "Modern 2 bedroom apartment in the heart of Sydney",
            PropertyManager = new PropertyManager
            {
                Name = "John Smith",
                Contact = "john.smith@realestate.com"
            }
        };

        var property2 = new RentalProperty
        {
            TenantId = _systemUserId.ToString(),
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
            Description = "Spacious 3 bedroom house with ocean views",
            PropertyManager = new PropertyManager
            {
                Name = "Sarah Johnson",
                Contact = "sarah.j@realestate.com"
            }
        };

        Console.WriteLine("Creating sample properties...");
        await propertiesCollection.InsertManyAsync(new[] { property1, property2 });
        Console.WriteLine($"Created {2} sample properties");

        // Create sample rental payments
        var paymentsCollection = _database.GetCollection<RentalPayment>(nameof(RentalPayment));
        var payments = new[]
        {
            new RentalPayment
            {
                TenantId = _systemUserId.ToString(),
                RentalPropertyId = property1.Id.ToString(),
                Amount = 2600.00M, // 4 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-5),
                PaymentMethodId = bankTransfer.Id.ToString(),
                PaymentReference = "RENT-123456",
                Notes = "Initial payment including bond"
            },
            new RentalPayment
            {
                TenantId = _systemUserId.ToString(),
                RentalPropertyId = property1.Id.ToString(),
                Amount = 1300.00M, // 2 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-4),
                PaymentMethodId = bankTransfer.Id.ToString(),
                PaymentReference = "RENT-123457"
            },
            new RentalPayment
            {
                TenantId = _systemUserId.ToString(),
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
                TenantId = _systemUserId.ToString(),
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
                TenantId = _systemUserId.ToString(),
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