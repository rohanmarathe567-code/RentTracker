using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace RentTrackerClient.Services;

public interface IAuthenticationService
{
    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email, string password, string confirmPassword);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    bool IsAuthenticated { get; }
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public AuthenticationService(
        HttpClient httpClient, 
        string baseUrl, 
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _authStateProvider = authStateProvider;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/login", new
            {
                Email = email,
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Login failed with status code: {response.StatusCode}");
                return false;
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse?.Token == null)
            {
                _logger.LogWarning("Login response did not contain a token");
                return false;
            }

            _authToken = loginResponse.Token;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

            // Force auth state to update
            if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                await customProvider.UpdateAuthenticationState(loginResponse.Token);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password, string confirmPassword)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/register", new
            {
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword,
                UserType = "User"
            });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Force auth state to update
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            await customProvider.UpdateAuthenticationState(null);
        }
    }

    public Task<string?> GetTokenAsync()
    {
        return Task.FromResult(_authToken);
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}