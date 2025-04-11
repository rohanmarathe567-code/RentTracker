using RentTrackerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace RentTrackerBackend.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;

    public DatabaseSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Only seed if the database is empty
        if (await _context.RentalProperties.AnyAsync())
        {
            return;
        }

        // Create payment methods
        var paymentMethods = new[]
        {
            new PaymentMethod
            {
                Name = "Bank Transfer",
                Description = "Direct bank transfer payment",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                Name = "Credit Card",
                Description = "Payment via credit card",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                Name = "Cash",
                Description = "Cash payment",
                IsSystemDefault = true
            },
            new PaymentMethod
            {
                Name = "PayPal",
                Description = "Payment through PayPal service",
                IsSystemDefault = true
            }
        };

        await _context.PaymentMethods.AddRangeAsync(paymentMethods);
        await _context.SaveChangesAsync();

        var bankTransfer = paymentMethods[0]; // Keep reference for sample payments

        // Create sample rental properties
        var property1 = new RentalProperty
        {
            Address = "123 Main Street",
            Suburb = "Sydney",
            State = "NSW",
            PostCode = "2000",
            Description = "Modern 2 bedroom apartment in the heart of Sydney",
            WeeklyRentAmount = 650.00M,
            LeaseStartDate = DateTime.UtcNow.AddMonths(-6),
            LeaseEndDate = DateTime.UtcNow.AddMonths(6),
            PropertyManager = "John Smith",
            PropertyManagerContact = "john.smith@realestate.com"
        };

        var property2 = new RentalProperty
        {
            Address = "45 Beach Road",
            Suburb = "Bondi",
            State = "NSW",
            PostCode = "2026",
            Description = "Spacious 3 bedroom house with ocean views",
            WeeklyRentAmount = 950.00M,
            LeaseStartDate = DateTime.UtcNow.AddMonths(-2),
            LeaseEndDate = DateTime.UtcNow.AddMonths(10),
            PropertyManager = "Sarah Johnson",
            PropertyManagerContact = "sarah.j@realestate.com"
        };

        await _context.RentalProperties.AddRangeAsync(property1, property2);
        await _context.SaveChangesAsync();

        // Create sample rental payments
        var payments = new[]
        {
            new RentalPayment
            {
                RentalPropertyId = property1.Id,
                Amount = 2600.00M, // 4 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-5),
                PaymentMethodId = bankTransfer.Id,
                PaymentReference = "RENT-123456",
                Notes = "Initial payment including bond"
            },
            new RentalPayment
            {
                RentalPropertyId = property1.Id,
                Amount = 1300.00M, // 2 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-4),
                PaymentMethodId = bankTransfer.Id,
                PaymentReference = "RENT-123457"
            },
            new RentalPayment
            {
                RentalPropertyId = property2.Id,
                Amount = 3800.00M, // 4 weeks rent
                PaymentDate = DateTime.UtcNow.AddMonths(-2),
                PaymentMethodId = bankTransfer.Id,
                PaymentReference = "RENT-123458",
                Notes = "Initial payment including bond"
            }
        };

        await _context.RentalPayments.AddRangeAsync(payments);
        await _context.SaveChangesAsync();

        // Create sample attachments
        var attachments = new[]
        {
            new Attachment
            {
                FileName = "lease_agreement.pdf",
                ContentType = "application/pdf",
                StoragePath = "/storage/property/lease_agreement.pdf",
                FileSize = 1024 * 1024, // 1MB
                Description = "Signed lease agreement",
                EntityType = "Property",
                RentalPropertyId = property1.Id,
                Tags = new[] { "lease", "agreement", "signed" }
            },
            new Attachment
            {
                FileName = "payment_receipt.pdf",
                ContentType = "application/pdf",
                StoragePath = "/storage/payment/receipt.pdf",
                FileSize = 512 * 1024, // 512KB
                Description = "Payment receipt",
                EntityType = "Payment",
                RentalPaymentId = payments[0].Id,
                Tags = new[] { "receipt", "payment" }
            }
        };

        await _context.Attachments.AddRangeAsync(attachments);
        await _context.SaveChangesAsync();
    }
}