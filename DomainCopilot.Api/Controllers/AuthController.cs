using System.Security.Claims;
using DomainCopilot.Application.Auth.DTOs;
using DomainCopilot.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await _authService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Username and password are required."
            });
        }

        var response = await _authService.AuthenticateAsync(request, cancellationToken);
        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Username and password are required."
            });
        }

        var response = await _authService.RegisterAsync(request, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        var fullName = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new
        {
            username,
            fullName,
            role,
            isAuthenticated = true
        });
    }
}
