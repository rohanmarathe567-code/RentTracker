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
    }    public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching payment methods");
            // Ensure the auth token is available
            var token = await _authService.GetTokenAsync();
            _logger.LogDebug($"Auth token present: {!string.IsNullOrEmpty(token)}");
            
            var methods = await GetListAsync<PaymentMethod>("");
            _logger.LogInformation($"Successfully fetched {methods.Count} payment methods");
            return methods;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP error fetching payment methods: {ex.StatusCode}");
            return new List<PaymentMethod>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching payment methods: {ex.Message}");
            return new List<PaymentMethod>();
        }
    }

    public async Task<PaymentMethod?> CreatePaymentMethodAsync(PaymentMethod paymentMethod)
    {
        try
        {
            return await PostAsync<PaymentMethod>("", paymentMethod);
        }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment method");
                return null;
            }
        }
        
        // Helper method to check if payment methods are available
        public async Task<bool> HasPaymentMethodsAsync()
        {
            var methods = await GetPaymentMethodsAsync();
            return methods.Count > 0;
        }
    }