using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using MongoDB.Driver;

namespace RentTrackerBackend.Services
{
    public interface IPaymentService
    {
        Task<RentalPayment?> GetPaymentByIdAsync(string tenantId, string paymentId);
        Task<IEnumerable<RentalPayment>> GetPaymentsByPropertyAsync(string tenantId, string propertyId, bool includeSystem = true, string[]? includes = null);
        Task<RentalPayment?> UpdatePaymentAsync(string tenantId, string paymentId, RentalPayment updatedPayment);
        Task<bool> DeletePaymentAsync(string tenantId, string paymentId);
        Task<RentalPayment> CreatePaymentAsync(RentalPayment payment);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMongoRepository<RentalProperty> _propertyRepository;

        public PaymentService(IPaymentRepository paymentRepository, IMongoRepository<RentalProperty> propertyRepository)
        {
            _paymentRepository = paymentRepository;
            _propertyRepository = propertyRepository;
        }

        public async Task<RentalPayment?> GetPaymentByIdAsync(string tenantId, string paymentId)
        {
            return await _paymentRepository.GetByIdAsync(tenantId, paymentId);
        }

        private async Task ValidatePropertyExistsAsync(string tenantId, string propertyId)
        {
            var property = await _propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
            {
                throw new ArgumentException($"Property with ID {propertyId} not found.");
            }
        }

        public async Task<IEnumerable<RentalPayment>> GetPaymentsByPropertyAsync(string tenantId, string propertyId, bool includeSystem = true, string[]? includes = null)
        {
            await ValidatePropertyExistsAsync(tenantId, propertyId);
            var payments = await _paymentRepository.GetAllAsync(tenantId, includeSystem, includes);
            return payments.Where(p => p.RentalPropertyId == propertyId);
        }

        public async Task<RentalPayment?> UpdatePaymentAsync(string tenantId, string paymentId, RentalPayment updatedPayment)
        {
            var existingPayment = await _paymentRepository.GetByIdAsync(tenantId, paymentId);
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

            await _paymentRepository.UpdateAsync(tenantId, paymentId, existingPayment);
            return existingPayment;
        }

        public async Task<bool> DeletePaymentAsync(string tenantId, string paymentId)
        {
            try
            {
                await _paymentRepository.DeleteAsync(tenantId, paymentId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RentalPayment> CreatePaymentAsync(RentalPayment payment)
        {
            await ValidatePropertyExistsAsync(payment.TenantId, payment.RentalPropertyId);

            // Ensure PaymentDate is in UTC
            payment.PaymentDate = payment.PaymentDate.Kind == DateTimeKind.Utc
                ? payment.PaymentDate
                : DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc);

            return await _paymentRepository.CreateAsync(payment);
        }
    }
}