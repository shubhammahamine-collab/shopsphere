using ShopSphere.Application.Abstractions;
using ShopSphere.Domain.Common;
using System.Security.Claims;

namespace ShopSphere.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "User is not authenticated.");
            }

            return int.Parse(userId);
        }
    }

    public string Role
    {
        get
        {
            return _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.Role)
                ?? string.Empty;
        }
    }

    public bool IsAdmin => Role == UserRoles.Admin;
}