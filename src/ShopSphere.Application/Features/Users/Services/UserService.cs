using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Users.DTOs;
using ShopSphere.Application.Features.Users.Requests;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Users.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        ILogger<UserService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<UserDto> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Attempting to register user with email {Email}.",
            email);

        var emailExists = await _context.Users
            .AnyAsync(
                x => x.Email == email,
                cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning(
                "Registration failed because email {Email} already exists.",
                email);

            throw new ArgumentException(
                "A user with this email already exists.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} registered successfully with email {Email} and role {Role}.",
            user.Id,
            user.Email,
            user.Role);

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<LoginResponseDto?> LoginAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == email,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning(
                "Login failed for email {Email} because the user was not found.",
                email);

            return null;
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning(
                "Login failed for email {Email} because the password was invalid.",
                email);

            return null;
        }

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation(
            "User {UserId} logged in successfully with role {Role}.",
            user.Id,
            user.Role);

        return new LoginResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role
            },
            Token = token
        };
    }
}