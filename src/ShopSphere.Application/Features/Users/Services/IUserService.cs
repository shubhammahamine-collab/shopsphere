using ShopSphere.Application.Features.Users.DTOs;
using ShopSphere.Application.Features.Users.Requests;

namespace ShopSphere.Application.Features.Users.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserRequest request);

    Task<LoginResponseDto?> LoginAsync(LoginUserRequest request);
}