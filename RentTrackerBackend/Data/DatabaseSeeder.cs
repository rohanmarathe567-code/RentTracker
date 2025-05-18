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

    private async Task DropCollectionsAsync()
    {
        // Drop all collections to ensure clean state
        Console.WriteLine("Dropping existing collections...");
        try
        {
            await _database.DropCollectionAsync(nameof(User));
            await _database.DropCollectionAsync(nameof(RentalProperty));
            await _database.DropCollectionAsync(nameof(PaymentMethod));
            await _database.DropCollectionAsync(nameof(PropertyTransaction));
            await _database.DropCollectionAsync(nameof(PropertyTransactionCategory));
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
        await DropCollectionsAsync();

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
        var paymentMethodsCollection = _database.GetCollection<PaymentMethod>(nameof(PaymentMethod));
        var categoriesCollection = _database.GetCollection<PropertyTransactionCategory>(nameof(PropertyTransactionCategory));

        // Create payment methods
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

        // Create transaction categories
        await SeedTransactionCategoriesAsync(categoriesCollection);
        
        // Create sample property transactions
        await SeedSampleTransactionsAsync(property1.Id.ToString(), property2.Id.ToString(), bankTransfer.Id.ToString());

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
                EntityType = "Transaction",
                TransactionId = property1.Id.ToString(),
                Tags = new[] { "receipt", "payment" }
            }
        };

        await attachmentsCollection.InsertManyAsync(attachments);

        // Update properties with payment and attachment references
        var updateProperty1 = Builders<RentalProperty>.Update
            .Push(p => p.AttachmentIds, attachments[0].Id.ToString());

        await propertiesCollection.UpdateOneAsync(
            p => p.Id == property1.Id,
            updateProperty1);
    }

    private async Task SeedTransactionCategoriesAsync(IMongoCollection<PropertyTransactionCategory> categoriesCollection)
    {
        Console.WriteLine("Creating transaction categories...");
        
        // Income Categories
        var incomeCategories = new List<PropertyTransactionCategory>
        {
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Weekly Rent",
                Description = "Weekly rental payment",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 1
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Monthly Rent",
                Description = "Monthly rental payment",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 2
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Advance Rent",
                Description = "Advance rental payment",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 3
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Late Payment Fees",
                Description = "Fees charged for late payments",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 4
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Bond/Deposit",
                Description = "Security bond or deposit",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 5
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Bond Claims",
                Description = "Claims against security bond",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 6
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Insurance Claims",
                Description = "Income from insurance claims",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 7
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Rental Subsidies",
                Description = "Government subsidies or rental assistance",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 8
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Miscellaneous Income",
                Description = "Other miscellaneous property income",
                TransactionType = TransactionType.Income,
                IsSystemDefault = true,
                Order = 9
            }
        };

        await categoriesCollection.InsertManyAsync(incomeCategories);
        
        // Expense Categories
        var expenseCategories = new List<PropertyTransactionCategory>
        {
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Strata Fees",
                Description = "Strata or body corporate fees",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 10
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Council Rates",
                Description = "Local council rates and charges",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 11
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Property Tax",
                Description = "Property tax payments",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 12
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Land Tax",
                Description = "Land tax payments",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 13
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Water",
                Description = "Water bills",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 14
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Gas",
                Description = "Gas bills",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 15
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Electricity",
                Description = "Electricity bills",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 16
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Internet/NBN",
                Description = "Internet and NBN bills",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 17
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Mortgage/Loan Payments",
                Description = "Mortgage or loan repayments",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 18
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Bank Fees",
                Description = "Bank fees and charges",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 19
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Insurance Premiums",
                Description = "Insurance premium payments",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 20
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Property Management Fees",
                Description = "Fees paid to property managers",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 21
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Repairs",
                Description = "General repair costs",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 22
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Regular Maintenance",
                Description = "Regular property maintenance",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 23
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Emergency Repairs",
                Description = "Emergency repair costs",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 24
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Legal Fees",
                Description = "Legal service expenses",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 25
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Advertising",
                Description = "Property advertising costs",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 26
            },
            new PropertyTransactionCategory
            {
                TenantId = "system",
                Name = "Miscellaneous Expenses",
                Description = "Other miscellaneous expenses",
                TransactionType = TransactionType.Expense,
                IsSystemDefault = true,
                Order = 27
            }
        };

        await categoriesCollection.InsertManyAsync(expenseCategories);
        Console.WriteLine("Transaction categories created successfully");
    }

    private async Task SeedSampleTransactionsAsync(string property1Id, string property2Id, string paymentMethodId)
    {
        Console.WriteLine("Creating sample property transactions...");
        
        var categoriesCollection = _database.GetCollection<PropertyTransactionCategory>(nameof(PropertyTransactionCategory));
        var transactionsCollection = _database.GetCollection<PropertyTransaction>(nameof(PropertyTransaction));
        
        // Get some category IDs for sample transactions
        var incomeCategories = await categoriesCollection.Find(c => c.TransactionType == TransactionType.Income).ToListAsync();
        var expenseCategories = await categoriesCollection.Find(c => c.TransactionType == TransactionType.Expense).ToListAsync();
        
        // Get specific categories
        var weeklyRent = incomeCategories.FirstOrDefault(c => c.Name == "Weekly Rent")
            ?? throw new InvalidOperationException("Weekly Rent category not found");
        var monthlyRent = incomeCategories.FirstOrDefault(c => c.Name == "Monthly Rent")
            ?? throw new InvalidOperationException("Monthly Rent category not found");
        var bondDeposit = incomeCategories.FirstOrDefault(c => c.Name == "Bond/Deposit")
            ?? throw new InvalidOperationException("Bond/Deposit category not found");
        var waterBill = expenseCategories.FirstOrDefault(c => c.Name == "Water")
            ?? throw new InvalidOperationException("Water category not found");
        var repairs = expenseCategories.FirstOrDefault(c => c.Name == "Repairs")
            ?? throw new InvalidOperationException("Repairs category not found");
        var councilRates = expenseCategories.FirstOrDefault(c => c.Name == "Council Rates")
            ?? throw new InvalidOperationException("Council Rates category not found");
        
        // Create sample transactions
        var transactions = new List<PropertyTransaction>();
        
        // Property 1 - Income Transactions
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property1Id,
            Amount = 2600.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-5),
            TransactionType = TransactionType.Income,
            CategoryId = bondDeposit.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "BOND-123456",
            Notes = "Initial bond payment"
        });
        
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property1Id,
            Amount = 650.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-5),
            TransactionType = TransactionType.Income,
            CategoryId = weeklyRent.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "RENT-123456",
            Notes = "First week's rent"
        });
        
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property1Id,
            Amount = 1300.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-4),
            TransactionType = TransactionType.Income,
            CategoryId = weeklyRent.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "RENT-123457",
            Notes = "Two weeks rent"
        });
        
        // Property 1 - Expense Transactions
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property1Id,
            Amount = 120.50M,
            TransactionDate = DateTime.UtcNow.AddMonths(-4).AddDays(5),
            TransactionType = TransactionType.Expense,
            CategoryId = waterBill.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "WATER-123456",
            Notes = "Quarterly water bill"
        });
        
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property1Id,
            Amount = 250.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-3),
            TransactionType = TransactionType.Expense,
            CategoryId = repairs.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "REPAIR-123456",
            Notes = "Plumbing repair in kitchen"
        });
        
        // Property 2 - Income Transactions
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property2Id,
            Amount = 3800.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-2),
            TransactionType = TransactionType.Income,
            CategoryId = bondDeposit.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "BOND-123457",
            Notes = "Initial bond payment"
        });
        
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property2Id,
            Amount = 3800.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-1),
            TransactionType = TransactionType.Income,
            CategoryId = monthlyRent.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "RENT-123458",
            Notes = "Monthly rent payment"
        });
        
        // Property 2 - Expense Transactions
        transactions.Add(new PropertyTransaction
        {
            TenantId = _systemUserId.ToString(),
            RentalPropertyId = property2Id,
            Amount = 450.00M,
            TransactionDate = DateTime.UtcNow.AddMonths(-1).AddDays(15),
            TransactionType = TransactionType.Expense,
            CategoryId = councilRates.Id.ToString(),
            PaymentMethodId = paymentMethodId,
            Reference = "COUNCIL-123456",
            Notes = "Quarterly council rates"
        });
        
        await transactionsCollection.InsertManyAsync(transactions);
        Console.WriteLine($"Created {transactions.Count} sample transactions");
    }
}