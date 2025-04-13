using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;

namespace RentTrackerClient.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private ClaimsPrincipal? _currentUser;
    private const string TokenKey = "authToken";

    private bool _isInitialized;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    private async Task InitializeAuthenticationStateAsync()
    {
        var token = await GetPersistedTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            SetAuthenticatedUser(token);
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAuthenticationStateAsync();
            _isInitialized = true;
        }
        return new AuthenticationState(_currentUser ?? _anonymous);
    }

    private async Task<string?> GetPersistedTokenAsync()
    {
        return await _localStorage.GetItemAsync(TokenKey);
    }

    private async Task PersistTokenAsync(string? token)
    {
        if (token == null)
        {
            await _localStorage.RemoveItemAsync(TokenKey);
        }
        else
        {
            await _localStorage.SetItemAsync(TokenKey, token);
        }
    }

    private void SetAuthenticatedUser(string jwtToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            var identity = new ClaimsIdentity(token.Claims, "jwt");
            _currentUser = new ClaimsPrincipal(identity);
        }
        catch
        {
            _currentUser = _anonymous;
        }
    }

    public async Task UpdateAuthenticationState(string? jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            _currentUser = _anonymous;
            await PersistTokenAsync(null);
        }
        else
        {
            SetAuthenticatedUser(jwtToken);
            await PersistTokenAsync(jwtToken);
        }

        var authState = Task.FromResult(new AuthenticationState(_currentUser ?? _anonymous));
        NotifyAuthenticationStateChanged(authState);
    }
}