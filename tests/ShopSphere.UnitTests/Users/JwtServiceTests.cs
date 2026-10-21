using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShopSphere.Application.Features.Users.DTOs;
using ShopSphere.Application.Features.Users.Services;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShopSphere.UnitTests.Users;

public class JwtServiceTests
{
    private const string TestKey =
        "ShopSphere-Unit-Test-Key-12345678901234567890";

    private readonly JwtService _sut;
    private readonly JwtSettings _settings;

    public JwtServiceTests()
    {
        _settings = new JwtSettings
        {
            Key = TestKey,
            Issuer = "ShopSphere.UnitTests",
            Audience = "ShopSphere.UnitTests.Client",
            ExpiryMinutes = 30
        };

        _sut = new JwtService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = UserRoles.Customer
        };

        // Act
        var tokenString = _sut.GenerateToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));

        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(_settings.Issuer, token.Issuer);
        Assert.Contains(_settings.Audience, token.Audiences);
        Assert.Equal("HS256", token.Header.Alg);
    }

    [Fact]
    public void GenerateToken_ValidUser_IncludesExpectedClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = UserRoles.Customer
        };

        // Act
        var tokenString = _sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(tokenString);

        // Assert
        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier
                && claim.Value == "42");

        Assert.Contains(
            token.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Email
                && claim.Value == "john@example.com");

        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.Name
                && claim.Value == "John Smith");

        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.Role
                && claim.Value == UserRoles.Customer);
    }


    [Fact]
    public void GenerateToken_ValidUser_SetsCorrectExpiration()
    {
        // Arrange
        var beforeGeneration = DateTime.UtcNow;

        var user = new User
        {
            Id = 100,
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice@example.com",
            Role = UserRoles.Customer
        };

        // Act
        var tokenString = _sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(tokenString);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        var expectedExpiryBefore = beforeGeneration
            .AddMinutes(_settings.ExpiryMinutes)
            .AddSeconds(-2);

        var expectedExpiryAfter = afterGeneration
            .AddMinutes(_settings.ExpiryMinutes)
            .AddSeconds(2);

        Assert.InRange(
            token.ValidTo,
            expectedExpiryBefore,
            expectedExpiryAfter);
    }

    [Theory]
    [InlineData(UserRoles.Customer)]
    [InlineData(UserRoles.Admin)]
    public void GenerateToken_DifferentRoles_IncludesCorrectRoleClaim(
        string role)
    {
        // Arrange
        var user = new User
        {
            Id = 200,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Role = role
        };

        // Act
        var tokenString = _sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(tokenString);

        // Assert
        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.Role
                && claim.Value == role);
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProducesTheirOwnClaims()
    {
        // Arrange
        var firstUser = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice@example.com",
            Role = UserRoles.Customer
        };

        var secondUser = new User
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Green",
            Email = "bob@example.com",
            Role = UserRoles.Admin
        };

        // Act
        var handler = new JwtSecurityTokenHandler();

        var firstToken = handler.ReadJwtToken(
            _sut.GenerateToken(firstUser));

        var secondToken = handler.ReadJwtToken(
            _sut.GenerateToken(secondUser));

        // Assert
        Assert.Contains(
            firstToken.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier
                && claim.Value == "1");

        Assert.Contains(
            firstToken.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Email
                && claim.Value == "alice@example.com");

        Assert.Contains(
            secondToken.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier
                && claim.Value == "2");

        Assert.Contains(
            secondToken.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Email
                && claim.Value == "bob@example.com");

        Assert.NotEqual(firstToken.RawData, secondToken.RawData);
    }

    [Fact]
    public void GenerateToken_ValidConfiguration_PassesSignatureIssuerAndAudienceValidation()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = UserRoles.Customer
        };

        var tokenString = _sut.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key)),
            ClockSkew = TimeSpan.Zero
        };

        // Act
        var principal = handler.ValidateToken(
            tokenString,
            validationParameters,
            out var validatedToken);

        // Assert
        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal(
            "42",
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        Assert.Equal(
            UserRoles.Customer,
            principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void GenerateToken_WrongSigningKey_FailsSignatureValidation()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = UserRoles.Customer
        };

        var tokenString = _sut.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "Different-Unit-Test-Key-12345678901234567890")),
            ClockSkew = TimeSpan.Zero
        };

        // Act and Assert
        var exception = Record.Exception(
            () => handler.ValidateToken(
                tokenString,
                validationParameters,
                out _));

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<SecurityTokenException>(exception);
    }

    [Fact]
    public void GenerateToken_WrongIssuer_FailsIssuerValidation()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = UserRoles.Customer
        };

        var tokenString = _sut.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "WrongIssuer",
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key)),
            ClockSkew = TimeSpan.Zero
        };

        // Act and Assert
        Assert.Throws<SecurityTokenInvalidIssuerException>(
            () => handler.ValidateToken(
                tokenString,
                validationParameters,
                out _));
    }

}
