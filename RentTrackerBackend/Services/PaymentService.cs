using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RentalPayment?> GetPaymentByIdAsync(Guid paymentId)
    {
        return await _context.RentalPayments
            .AsNoTracking()
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    private async Task ValidatePropertyExistsAsync(Guid propertyId)
    {
        var exists = await _context.RentalProperties
            .AsNoTracking()
            .AnyAsync(p => p.Id == propertyId);

        if (!exists)
        {
            throw new ArgumentException($"Property with ID {propertyId} not found.");
        }
    }

    public async Task<IQueryable<RentalPayment>> GetPaymentsByPropertyQueryAsync(Guid propertyId)
    {
        await ValidatePropertyExistsAsync(propertyId);

        return _context.RentalPayments
            .AsNoTracking()
            .Include(p => p.PaymentMethod)
            .Where(p => p.RentalPropertyId == propertyId)
            .OrderByDescending(p => p.PaymentDate);
    }

    public async Task<RentalPayment?> UpdatePaymentAsync(Guid paymentId, RentalPayment updatedPayment)
    {
        var existingPayment = await _context.RentalPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (existingPayment == null)
        {
            return null;
        }

        // Update only the modifiable fields
        existingPayment.Amount = updatedPayment.Amount;
        existingPayment.PaymentDate = updatedPayment.PaymentDate.Kind == DateTimeKind.Utc
            ? updatedPayment.PaymentDate
            : DateTime.SpecifyKind(updatedPayment.PaymentDate, DateTimeKind.Utc);
        existingPayment.PaymentMethodId = updatedPayment.PaymentMethodId;
        existingPayment.PaymentReference = updatedPayment.PaymentReference;
        existingPayment.Notes = updatedPayment.Notes;
        existingPayment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingPayment;
    }

    public async Task<bool> DeletePaymentAsync(Guid paymentId)
    {
        var payment = await _context.RentalPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
        {
            return false;
        }

        _context.RentalPayments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<RentalPayment> CreatePaymentAsync(RentalPayment payment)
    {
        await ValidatePropertyExistsAsync(payment.RentalPropertyId);
// Always generate a new ID for new payments, regardless of what's provided
payment.Id = SequentialGuidGenerator.NewSequentialGuid();


        // Ensure PaymentDate is in UTC
        payment.PaymentDate = payment.PaymentDate.Kind == DateTimeKind.Utc
            ? payment.PaymentDate
            : DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc);

        // Set timestamps
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        _context.RentalPayments.Add(payment);
        await _context.SaveChangesAsync();
        
        return payment;
    }
}