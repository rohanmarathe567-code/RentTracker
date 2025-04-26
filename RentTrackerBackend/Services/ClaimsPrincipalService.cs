using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RentTrackerBackend.Services
{
    public interface IClaimsPrincipalService
    {
        string? GetTenantId();
        bool ValidateTenantId(out string tenantId);
        bool IsInRole(string role);
    }

    public class ClaimsPrincipalService : IClaimsPrincipalService
    {
        private readonly ILogger<ClaimsPrincipalService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsPrincipalService(
            ILogger<ClaimsPrincipalService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetTenantId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public bool ValidateTenantId(out string tenantId)
        {
            tenantId = GetTenantId() ?? string.Empty;
            
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("User ID claim not found");
                return false;
            }

            return true;
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }
    }
}