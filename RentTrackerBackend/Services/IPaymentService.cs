using RentTrackerBackend.Models;
using System.Linq;

namespace RentTrackerBackend.Services;

public interface IPaymentService
{
    Task<RentalPayment?> GetPaymentByIdAsync(Guid paymentId);
    Task<IQueryable<RentalPayment>> GetPaymentsByPropertyQueryAsync(Guid propertyId);
    Task<RentalPayment?> UpdatePaymentAsync(Guid paymentId, RentalPayment updatedPayment);
    Task<bool> DeletePaymentAsync(Guid paymentId);
}