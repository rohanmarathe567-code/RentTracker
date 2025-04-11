using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class PaymentMethodService : HttpClientService
{
    public PaymentMethodService(
        HttpClient httpClient,
        ILogger<PaymentMethodService> logger,
        IAuthenticationService authService)
        : base(httpClient, "api/paymentmethods", logger, authService)
    {
        _logger.LogInformation("PaymentMethodService initialized");
    }

    public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
    {
        try
        {
            return await GetListAsync<PaymentMethod>("");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment methods");
            return new List<PaymentMethod>();
        }
    }

    public async Task<PaymentMethod?> CreatePaymentMethodAsync(PaymentMethod paymentMethod)
    {
        try
        {
            return await PostAsync<PaymentMethod>("", paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return null;
        }
    }
}