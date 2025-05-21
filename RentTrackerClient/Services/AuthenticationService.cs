using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace RentTrackerClient.Services;

public interface IAuthenticationService
{
    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string firstName, string? middleName, string lastName, string email, string password, string confirmPassword);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<string> GetCurrentTenantIdAsync();
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
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public AuthenticationService(
        HttpClient httpClient, 
        string baseUrl, 
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationService> logger,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _authStateProvider = authStateProvider;
        _logger = logger;
        _localStorage = localStorage;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        // Note: Initialization will happen when GetTokenAsync is first called
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var loginRequest = new { Email = email.Trim(), Password = password };
            _logger.LogDebug($"Sending login request: {System.Text.Json.JsonSerializer.Serialize(loginRequest)}");
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/login", loginRequest);

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

            // Store token in localStorage
            await _localStorage.SetItemAsync(TokenKey, _authToken);

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

    public async Task<bool> RegisterAsync(string firstName, string? middleName, string lastName, string email, string password, string confirmPassword)
    {
        try
        {
            var registerRequest = new
            {
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Email = email.Trim(),
                Password = password,
                ConfirmPassword = confirmPassword,
                UserType = "User"
            };
            _logger.LogDebug($"Sending register request: {System.Text.Json.JsonSerializer.Serialize(registerRequest)}");
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/register", registerRequest);

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
        await _localStorage.RemoveItemAsync(TokenKey);

        // Force auth state to update
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            await customProvider.UpdateAuthenticationState(null);
        }
    }    private bool _initialized;
    private Task? _initializationTask;

    public async Task<string?> GetTokenAsync()
    {
        if (!_initialized)
        {
            if (_initializationTask == null)
            {
                _initializationTask = InitializeAuthTokenAsync();
            }
            await _initializationTask;
            _initialized = true;
        }
        return _authToken;
    }
      public Task<string> GetCurrentTenantIdAsync()
    {
        // For now, we'll return a default tenant ID since the system is tenant-enabled
        // In a real implementation, this would extract the tenant ID from the JWT token
        // or from another storage mechanism
        return Task.FromResult("default-tenant");
    }

    private async Task InitializeAuthTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                _authToken = token;
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

                // Force auth state to update if needed
                if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
                {
                    await customProvider.UpdateAuthenticationState(token);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize auth token");
        }
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
}