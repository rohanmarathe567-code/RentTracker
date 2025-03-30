using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public interface IPaymentService
{
    Task<bool> ValidatePropertyExistsAsync(int propertyId);
    Task<RentalPayment> CreatePaymentAsync(RentalPayment payment);
    Task<RentalPayment?> UpdatePaymentAsync(int id, RentalPayment updatedPayment);
    Task<bool> DeletePaymentAsync(int id);
    Task<RentalPayment?> GetPaymentByIdAsync(int id);
    Task<IEnumerable<RentalPayment>> GetPaymentsByPropertyIdAsync(int propertyId);
}