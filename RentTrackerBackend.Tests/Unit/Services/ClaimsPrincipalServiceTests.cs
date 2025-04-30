using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Tests.Unit.Services;

public class ClaimsPrincipalServiceTests
{
    private readonly ILogger<ClaimsPrincipalService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ClaimsPrincipalService _service;

    public ClaimsPrincipalServiceTests()
    {
        _logger = Substitute.For<ILogger<ClaimsPrincipalService>>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _service = new ClaimsPrincipalService(_logger, _httpContextAccessor);
    }

    [Fact]
    public void GetTenantId_WhenUserHasNameIdentifierClaim_ReturnsTenantId()
    {
        // Arrange
        var expectedTenantId = "tenant123";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, expectedTenantId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.GetTenantId();

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public void GetTenantId_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _service.GetTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTenantId_WhenNoClaims_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.GetTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateTenantId_WhenTenantIdExists_ReturnsTrue()
    {
        // Arrange
        var expectedTenantId = "tenant123";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, expectedTenantId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.ValidateTenantId(out var tenantId);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedTenantId, tenantId);
    }

    [Fact]
    public void ValidateTenantId_WhenNoTenantId_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.ValidateTenantId(out var tenantId);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, tenantId);
        
        // Verify warning was logged
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(msg => msg.ToString()!.Contains("User ID claim not found")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void IsInRole_WhenUserHasRole_ReturnsTrue()
    {
        // Arrange
        var expectedRole = "Admin";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, expectedRole)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.IsInRole(expectedRole);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = _service.IsInRole("Admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WhenNoHttpContext_ReturnsFalse()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _service.IsInRole("Admin");

        // Assert
        Assert.False(result);
    }
}