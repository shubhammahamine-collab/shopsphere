using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShopSphere.Application.Features.Users.Requests;
using ShopSphere.Application.Features.Users.Services;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;

namespace ShopSphere.UnitTests.Users;

public class UserServiceTests
{
    private readonly ShopSphereDbContextStub _context;
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<ILogger<UserService>> _logger = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ShopSphereDbContextStub>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ShopSphereDbContextStub(options);

        _sut = new UserService(
            _context,
            _passwordHasher.Object,
            _jwtService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesCustomerAndReturnsDto()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            FirstName = "  John  ",
            LastName = "  Smith  ",
            Email = "  JOHN@EXAMPLE.COM  ",
            Password = "Password123!"
        };

        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<User>(),
                request.Password))
            .Returns("hashed-password");

        // Act
        var result = await _sut.RegisterAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal(UserRoles.Customer, result.Role);
        Assert.True(result.Id > 0);

        var savedUser = await _context.Users.SingleAsync();

        Assert.Equal("hashed-password", savedUser.PasswordHash);
        Assert.True(savedUser.IsActive);
        Assert.Equal(UserRoles.Customer, savedUser.Role);

        _passwordHasher.Verify(
            x => x.HashPassword(
                It.IsAny<User>(),
                request.Password),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsArgumentException()
    {
        // Arrange
        _context.Users.Add(new User
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "john@example.com",
            PasswordHash = "existing-hash",
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var request = new RegisterUserRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = " JOHN@EXAMPLE.COM ",
            Password = "Password123!"
        };

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RegisterAsync(
                request,
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        Assert.Single(await _context.Users.ToListAsync());

        _passwordHasher.Verify(
            x => x.HashPassword(
                It.IsAny<User>(),
                It.IsAny<string>()),
            Times.Never);
    }


    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUserAndToken()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            PasswordHash = "hashed-password",
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginUserRequest
        {
            Email = " JOHN@EXAMPLE.COM ",
            Password = "Password123!"
        };

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                "hashed-password",
                request.Password))
            .Returns(PasswordVerificationResult.Success);

        _jwtService
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        // Act
        var result = await _sut.LoginAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-jwt-token", result.Token);
        Assert.Equal("john@example.com", result.User.Email);
        Assert.Equal(user.Id, result.User.Id);

        _jwtService.Verify(
            x => x.GenerateToken(It.Is<User>(
                u => u.Id == user.Id)),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var request = new LoginUserRequest
        {
            Email = "missing@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.LoginAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Null(result);

        _passwordHasher.Verify(
            x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _jwtService.Verify(
            x => x.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ReturnsNull()
    {
        // Arrange
        _context.Users.Add(new User
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            PasswordHash = "hashed-password",
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var request = new LoginUserRequest
        {
            Email = "john@example.com",
            Password = "WrongPassword!"
        };

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                "hashed-password",
                request.Password))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var result = await _sut.LoginAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Null(result);

        _jwtService.Verify(
            x => x.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }


    [Theory]
    [InlineData(PasswordVerificationResult.Success)]
    [InlineData(PasswordVerificationResult.SuccessRehashNeeded)]
    public async Task LoginAsync_PasswordVerificationSucceeds_ReturnsToken(
        PasswordVerificationResult verificationResult)
    {
        // Arrange
        _context.Users.Add(new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            PasswordHash = "hashed-password",
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                "hashed-password",
                "Password123!"))
            .Returns(verificationResult);

        _jwtService
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        var request = new LoginUserRequest
        {
            Email = "jane@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.LoginAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-jwt-token", result.Token);

        _jwtService.Verify(
            x => x.GenerateToken(It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_EmailHasDifferentCaseAndSpaces_FindsUser()
    {
        // Arrange
        _context.Users.Add(new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            PasswordHash = "hashed-password",
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                "hashed-password",
                "Password123!"))
            .Returns(PasswordVerificationResult.Success);

        _jwtService
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        var request = new LoginUserRequest
        {
            Email = "  JANE@EXAMPLE.COM  ",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.LoginAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jane@example.com", result.User.Email);
    }

}
