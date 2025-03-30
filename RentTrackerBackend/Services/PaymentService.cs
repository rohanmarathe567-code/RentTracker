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

    public async Task<bool> ValidatePropertyExistsAsync(Guid propertyId)
    {
        return await _context.RentalProperties.AnyAsync(p => p.Id == propertyId);
    }

    public async Task<RentalPayment> CreatePaymentAsync(RentalPayment payment)
    {
        // Validate property exists
        if (!await ValidatePropertyExistsAsync(payment.RentalPropertyId))
        {
            throw new ArgumentException($"Property with ID {payment.RentalPropertyId} does not exist.");
        }

        // Set timestamps
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        // Add and save payment
        _context.RentalPayments.Add(payment);
        await _context.SaveChangesAsync();

        return payment;
    }

    public async Task<RentalPayment?> UpdatePaymentAsync(Guid id, RentalPayment updatedPayment)
    {
        var payment = await _context.RentalPayments.FindAsync(id);
        
        if (payment == null)
        {
            return null;
        }

        // Validate property exists if trying to change property
        if (updatedPayment.RentalPropertyId != payment.RentalPropertyId && 
            !await ValidatePropertyExistsAsync(updatedPayment.RentalPropertyId))
        {
            throw new ArgumentException($"Property with ID {updatedPayment.RentalPropertyId} does not exist.");
        }

        // Update payment details
        payment.Amount = updatedPayment.Amount;
        payment.PaymentDate = updatedPayment.PaymentDate;
        payment.PaymentMethod = updatedPayment.PaymentMethod;
        payment.PaymentReference = updatedPayment.PaymentReference;
        payment.Notes = updatedPayment.Notes;
        payment.RentalPropertyId = updatedPayment.RentalPropertyId;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return payment;
    }

    public async Task<bool> DeletePaymentAsync(Guid id)
    {
        var payment = await _context.RentalPayments.FindAsync(id);
        
        if (payment == null)
        {
            return false;
        }

        _context.RentalPayments.Remove(payment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<RentalPayment?> GetPaymentByIdAsync(Guid id)
    {
        return await _context.RentalPayments
            .Include(p => p.RentalProperty)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<RentalPayment>> GetPaymentsByPropertyIdAsync(Guid propertyId)
    {
        // Validate property exists
        if (!await ValidatePropertyExistsAsync(propertyId))
        {
            throw new ArgumentException($"Property with ID {propertyId} does not exist.");
        }

        return await _context.RentalPayments
            .Where(p => p.RentalPropertyId == propertyId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }
}