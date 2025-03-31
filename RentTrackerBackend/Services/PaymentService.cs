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
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<IQueryable<RentalPayment>> GetPaymentsByPropertyQueryAsync(Guid propertyId)
    {
        // Validate property exists
        var propertyExists = await _context.RentalProperties
            .AnyAsync(p => p.Id == propertyId);

        if (!propertyExists)
        {
            throw new ArgumentException($"Property with ID {propertyId} not found.");
        }

        return _context.RentalPayments
            .AsNoTracking()
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
        existingPayment.PaymentDate = updatedPayment.PaymentDate;
        existingPayment.PaymentMethod = updatedPayment.PaymentMethod;
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
}