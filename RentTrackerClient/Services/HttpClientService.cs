using System.Net.Http.Json;
using System.Text.Json;

namespace RentTrackerClient.Services;

public abstract class HttpClientService
{
    protected readonly HttpClient _httpClient;
    protected readonly string _baseUrl;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected HttpClientService(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    protected async Task<List<T>> GetListAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions) ?? new List<T>();
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/{endpoint}", data, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    protected async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{endpoint}", data, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    protected async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");
        response.EnsureSuccessStatusCode();
    }
}