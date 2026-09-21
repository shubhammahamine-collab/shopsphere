using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Users.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}