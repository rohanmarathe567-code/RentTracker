using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RentTrackerClient.Services;

public abstract class HttpClientService
{
    protected readonly HttpClient _httpClient;
    protected readonly string _baseUrl;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly ILogger _logger;

    protected HttpClientService(HttpClient httpClient, string baseUrl, ILogger logger)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var fullUrl = $"{_baseUrl}/{endpoint}";
            _logger.LogDebug($"GET Request: {fullUrl}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation($"GET Request to {fullUrl} completed in {duration.TotalMilliseconds}ms. Status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Raw Response Content: {responseContent}");

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                _logger.LogDebug($"GET Request Result: {JsonSerializer.Serialize(result)}");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON Deserialization Error. Raw Content: {responseContent}");
                throw new InvalidOperationException($"Failed to deserialize JSON. Raw content: {responseContent}", ex);
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
        try
        {
            var fullUrl = $"{_baseUrl}/{endpoint}";
            _logger.LogDebug($"GET List Request: {fullUrl}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation($"GET List Request to {fullUrl} completed in {duration.TotalMilliseconds}ms. Status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Raw List Response Content: {responseContent}");

            try
            {
                var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions) ?? new List<T>();
                _logger.LogDebug($"GET List Request Result: {result.Count} items");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON List Deserialization Error. Raw Content: {responseContent}");
                throw new InvalidOperationException($"Failed to deserialize JSON list. Raw content: {responseContent}", ex);
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
            var fullUrl = $"{_baseUrl}/{endpoint}";
            
            // Log the request details with better formatting
            var requestJson = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });
            _logger.LogDebug($"POST Request: {fullUrl}\nRequest Data: {requestJson}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PostAsJsonAsync(fullUrl, data, _jsonOptions);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation($"POST Request to {fullUrl} completed in {duration.TotalMilliseconds}ms. Status: {response.StatusCode}");

            // Read the response content regardless of status code
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // If the response is not successful, log the error details
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"POST Request failed with status code {response.StatusCode}. Response: {responseContent}");
                
                // Try to parse error details if available
                try
                {
                    var errorDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    if (errorDetails != null)
                    {
                        foreach (var error in errorDetails)
                        {
                            _logger.LogError($"Error detail - {error.Key}: {error.Value}");
                        }
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning($"Could not parse error response as JSON: {responseContent}");
                }
            }
            
            // Now ensure success status code (will throw if not successful)
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug($"Raw POST Response Content: {responseContent}");

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                _logger.LogDebug($"POST Request Result: {JsonSerializer.Serialize(result)}");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON POST Deserialization Error. Raw Content: {responseContent}");
                throw new InvalidOperationException($"Failed to deserialize JSON. Raw content: {responseContent}", ex);
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
            var fullUrl = $"{_baseUrl}/{endpoint}";
            _logger.LogDebug($"PUT Request: {fullUrl}. Data: {JsonSerializer.Serialize(data)}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PutAsJsonAsync(fullUrl, data, _jsonOptions);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation($"PUT Request to {fullUrl} completed in {duration.TotalMilliseconds}ms. Status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();
            
            // For 204 No Content responses, return null without attempting to deserialize
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogDebug("Server returned 204 No Content - no response body to deserialize");
                return default;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Raw PUT Response Content: {responseContent}");

            try
            {
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogDebug("Empty response content - returning default value");
                    return default;
                }
                
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                _logger.LogDebug($"PUT Request Result: {JsonSerializer.Serialize(result)}");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON PUT Deserialization Error. Raw Content: {responseContent}");
                throw new InvalidOperationException($"Failed to deserialize JSON. Raw content: {responseContent}", ex);
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
            var fullUrl = $"{_baseUrl}/{endpoint}";
            _logger.LogDebug($"DELETE Request: {fullUrl}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.DeleteAsync(fullUrl);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation($"DELETE Request to {fullUrl} completed in {duration.TotalMilliseconds}ms. Status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP DELETE request failed for endpoint: {endpoint}");
            throw;
        }
    }
}