namespace DomainCopilot.Application.Auth.DTOs;

public class LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public string? Username { get; init; }
    public string? Role { get; init; }
    public string? ErrorMessage { get; init; }
    public int ExpiresInSeconds { get; init; }
}

public static class UserRoles
{
    public const string Technician = "Technician";
    public const string Supervisor = "Supervisor";
}

public class RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = UserRoles.Technician;
}

public class UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
