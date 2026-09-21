using Microsoft.AspNetCore.Mvc;
using ShopSphere.API.Models;
using ShopSphere.Application.Features.Users.DTOs;
using ShopSphere.Application.Features.Users.Requests;
using ShopSphere.Application.Features.Users.Services;

namespace ShopSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        RegisterUserRequest request)
    {
        var user = await _userService.RegisterAsync(request);

        return Ok(user);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
    LoginUserRequest request)
    {
        var user = await _userService.LoginAsync(request);

        if (user == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Invalid email or password."
            });
        }

        return Ok(user);
    }
}