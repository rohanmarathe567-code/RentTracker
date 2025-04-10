using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class PaymentMethodService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentMethodService> _logger;

    public PaymentMethodService(HttpClient httpClient, ILogger<PaymentMethodService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/paymentmethods");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PaymentMethod>>() ?? new List<PaymentMethod>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment methods");
            return new List<PaymentMethod>();
        }
    }
}