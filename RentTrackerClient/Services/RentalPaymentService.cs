using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class RentalPaymentService : HttpClientService
{
    public RentalPaymentService(HttpClient httpClient, ILogger<RentalPaymentService> logger) 
        : base(httpClient, "api/payments", logger)
    {
    }

    public async Task<List<RentalPayment>> GetAllPaymentsAsync()
    {
        _logger.LogInformation("Fetching all rental payments");
        var payments = await GetListAsync<RentalPayment>("");
        _logger.LogDebug($"Retrieved {payments.Count} rental payments");
        return payments;
    }

    public async Task<RentalPayment?> GetPaymentAsync(int id)
    {
        _logger.LogInformation($"Fetching rental payment with ID: {id}");
        var payment = await GetAsync<RentalPayment>($"{id}");
        
        if (payment == null)
        {
            _logger.LogWarning($"No rental payment found with ID: {id}");
        }
        else
        {
            _logger.LogDebug($"Retrieved rental payment details for ID: {id}");
        }

        return payment;
    }

    public async Task<RentalPayment?> CreatePaymentAsync(RentalPayment payment)
    {
        _logger.LogInformation("Creating new rental payment");
        _logger.LogDebug($"Payment details: {System.Text.Json.JsonSerializer.Serialize(payment)}");
        
        var createdPayment = await PostAsync<RentalPayment>("", payment);
        
        if (createdPayment != null)
        {
            _logger.LogInformation($"Successfully created rental payment with ID: {createdPayment.Id}");
        }
        else
        {
            _logger.LogWarning("Failed to create rental payment");
        }

        return createdPayment;
    }

    public async Task<RentalPayment?> UpdatePaymentAsync(int id, RentalPayment payment)
    {
        _logger.LogInformation($"Updating rental payment with ID: {id}");
        _logger.LogDebug($"Updated payment details: {System.Text.Json.JsonSerializer.Serialize(payment)}");
        
        var updatedPayment = await PutAsync<RentalPayment>($"{id}", payment);
        
        if (updatedPayment != null)
        {
            _logger.LogInformation($"Successfully updated rental payment with ID: {id}");
        }
        else
        {
            _logger.LogWarning($"Failed to update rental payment with ID: {id}");
        }

        return updatedPayment;
    }

    public async Task DeletePaymentAsync(int id)
    {
        _logger.LogInformation($"Deleting rental payment with ID: {id}");
        
        try
        {
            await DeleteAsync($"{id}");
            _logger.LogInformation($"Successfully deleted rental payment with ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting rental payment with ID: {id}");
            throw;
        }
    }

    public async Task<List<RentalPayment>> GetPaymentsByPropertyAsync(int propertyId)
    {
        _logger.LogInformation($"Fetching payments for property with ID: {propertyId}");
        
        var payments = await GetListAsync<RentalPayment>($"property/{propertyId}");
        
        _logger.LogDebug($"Retrieved {payments.Count} payments for property with ID: {propertyId}");
        return payments;
    }

    public async Task<decimal> GetTotalPaymentsForPropertyAsync(int propertyId)
    {
        _logger.LogInformation($"Calculating total payments for property with ID: {propertyId}");
        
        try
        {
            var payments = await GetPaymentsByPropertyAsync(propertyId);
            var totalPayments = payments.Sum(p => p.Amount);
            
            _logger.LogDebug($"Total payments for property {propertyId}: {totalPayments:C}");
            return totalPayments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating total payments for property with ID: {propertyId}");
            throw;
        }
    }
}