namespace ShopSphere.Application.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }

    string Role { get; }

    bool IsAdmin { get; }
}