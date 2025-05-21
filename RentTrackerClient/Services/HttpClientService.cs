using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RentTrackerClient.Services;

public abstract class HttpClientService
{
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly ILogger _logger;
    protected readonly IAuthenticationService _authService;
    protected readonly string _baseUrl;

    protected HttpClientService(
        HttpClient httpClient,
        string baseUrl,
        ILogger logger,
        IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _logger = logger;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        await SetAuthHeaderAsync();
        try
        {
            var fullUrl = $"{_baseUrl}/{endpoint}".TrimEnd('/');
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug($"GET {fullUrl} - {response.StatusCode} ({duration.TotalMilliseconds}ms)");

            response.EnsureSuccessStatusCode();
            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON deserialization failed for GET {endpoint}");
                throw new InvalidOperationException("Failed to deserialize JSON response", ex);
            }
            // Removed redundant return statement
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP GET request failed for endpoint: {endpoint}");
            throw;
        }
    }

    protected async Task<List<T>> GetListAsync<T>(string endpoint)
    {
        await SetAuthHeaderAsync();
        try
        {
            var fullUrl = $"{_baseUrl}/{endpoint}".TrimEnd('/');
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug($"GET List {fullUrl} - {response.StatusCode} ({duration.TotalMilliseconds}ms)");

            response.EnsureSuccessStatusCode();
            try
            {
                var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions) ?? new List<T>();
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON list deserialization failed for GET {endpoint}");
                throw new InvalidOperationException("Failed to deserialize JSON list response", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP GET list request failed for endpoint: {endpoint}");
            throw;
        }
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var fullUrl = $"{_baseUrl}/{endpoint}".TrimEnd('/');
            
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PostAsJsonAsync(fullUrl, data, _jsonOptions);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug($"POST {fullUrl} - {response.StatusCode} ({duration.TotalMilliseconds}ms)");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"POST {fullUrl} failed: {response.StatusCode}");
            }

            response.EnsureSuccessStatusCode();
            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return result;
            }
            catch (JsonException ex)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(ex, $"JSON deserialization failed for POST {endpoint}");
                throw new InvalidOperationException("Failed to deserialize JSON response", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP POST request failed for endpoint: {endpoint}. Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error during POST request to endpoint: {endpoint}. Error: {ex.Message}");
            throw;
        }
    }

    protected async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var fullUrl = $"{_baseUrl}/{endpoint}".TrimEnd('/');
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PutAsJsonAsync(fullUrl, data, _jsonOptions);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug($"PUT {fullUrl} - {response.StatusCode} ({duration.TotalMilliseconds}ms)");

            response.EnsureSuccessStatusCode();
            
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return result;
            }
            catch (JsonException ex)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(ex, $"JSON deserialization failed for PUT {endpoint}");
                throw new InvalidOperationException("Failed to deserialize JSON response", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP PUT request failed for endpoint: {endpoint}");
            throw;
        }
    }

    protected async Task DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var fullUrl = $"{_baseUrl}/{endpoint}".TrimEnd('/');
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.DeleteAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug($"DELETE {fullUrl} - {response.StatusCode} ({duration.TotalMilliseconds}ms)");

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP DELETE request failed for endpoint: {endpoint}");
            throw;
        }
    }
    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}