namespace ShopSphere.Application.Features.Users.DTOs;

public class LoginResponseDto
{
    public UserDto User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
}