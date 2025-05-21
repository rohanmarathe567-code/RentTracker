using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NSubstitute;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models.Auth;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<User> _collection;
    private readonly JwtConfig _jwtConfig;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mongoClient = Substitute.For<IMongoClient>();
        _database = Substitute.For<IMongoDatabase>();
        _collection = Substitute.For<IMongoCollection<User>>();

        _mongoClient.GetDatabase("renttracker").Returns(_database);
        _database.GetCollection<User>(nameof(User)).Returns(_collection);

        _jwtConfig = new JwtConfig
        {
            Secret = "your-256-bit-secret-your-256-bit-secret-your-256-bit-secret",
            Issuer = "test-issuer",
            Audience = "test-audience",
            TokenLifetimeMinutes = 60
        };

        _authService = new AuthService(_database, _jwtConfig);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Setup empty result for email check
        var cursor = Substitute.For<IAsyncCursor<User>>();
        cursor.MoveNextAsync().Returns(true, false);
        cursor.Current.Returns(new List<User>());
        
        _collection.FindAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<FindOptions<User, User>>(),
            Arg.Any<CancellationToken>())
            .Returns(cursor);

        User? capturedUser = null;
        _collection.When(x => x.InsertOneAsync(
            Arg.Any<User>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>()))
            .Do(x => capturedUser = x.Arg<User>());

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.NotNull(result.Token);
        Assert.NotEqual(request.Password, capturedUser?.PasswordHash); // Password should be hashed
        Assert.Equal(UserType.User, result.UserType);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var cursor = Substitute.For<IAsyncCursor<User>>();
        cursor.MoveNextAsync().Returns(Task.FromResult(true));
        cursor.Current.Returns(new List<User> { new User { Email = request.Email } });

        _collection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(1L);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(loginRequest.Password);
        var user = new User
        {
            Email = loginRequest.Email,
            PasswordHash = hashedPassword,
            FirstName = "Test",
            LastName = "User",
            UserType = UserType.User
        };

        var cursor = Substitute.For<IAsyncCursor<User>>();
        cursor.MoveNextAsync().Returns(true, false);
        cursor.Current.Returns(new List<User> { user });

        _collection.FindAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<FindOptions<User, User>>(),
            Arg.Any<CancellationToken>())
            .Returns(cursor);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new User
        {
            Email = loginRequest.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DifferentPassword123!"),
            FirstName = "Test",
            LastName = "User"
        };

        var cursor = Substitute.For<IAsyncCursor<User>>();
        cursor.MoveNextAsync().Returns(true, false);
        cursor.Current.Returns(new List<User> { user });

        _collection.FindAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<FindOptions<User, User>>(),
            Arg.Any<CancellationToken>())
            .Returns(cursor);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync(loginRequest));
    }

    [Fact]
    public void GenerateToken_CreatesValidJwtWithExpectedClaims()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserType = UserType.User
        };

        // Act
        var token = _authService.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtConfig.Issuer, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iss).Value);
        Assert.Equal(_jwtConfig.Audience, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Aud).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.FirstName, jwtToken.Claims.First(c => c.Type == ClaimTypes.GivenName).Value);
        Assert.Equal(user.LastName, jwtToken.Claims.First(c => c.Type == ClaimTypes.Surname).Value);
        Assert.Equal(user.UserType.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void HashPassword_CreatesValidBCryptHash()
    {
        // Arrange
        var password = "Password123!";

        // Act
        var hash = _authService.HashPassword(password);

        // Assert
        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
    }

    [Fact]
    public void VerifyPassword_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        var password = "Password123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var password = "Password123!";
        var hash = BCrypt.Net.BCrypt.HashPassword("DifferentPassword123!");

        // Act
        var result = _authService.VerifyPassword(password, hash);

        // Assert
        Assert.False(result);
    }
}