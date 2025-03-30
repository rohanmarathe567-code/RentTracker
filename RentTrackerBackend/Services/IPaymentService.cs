using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public interface IPaymentService
{
    Task<bool> ValidatePropertyExistsAsync(Guid propertyId);
    Task<RentalPayment> CreatePaymentAsync(RentalPayment payment);
    Task<RentalPayment?> UpdatePaymentAsync(Guid id, RentalPayment updatedPayment);
    Task<bool> DeletePaymentAsync(Guid id);
    Task<RentalPayment?> GetPaymentByIdAsync(Guid id);
    Task<IEnumerable<RentalPayment>> GetPaymentsByPropertyIdAsync(Guid propertyId);
}