using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DomainCopilot.Application.Auth.DTOs;
using DomainCopilot.Application.Auth.Interfaces;
using DomainCopilot.Domain.Users;
using DomainCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DomainCopilot.Infrastructure.Auth;

public class JwtAuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _expiryMinutes;

    public JwtAuthService(
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _issuer = configuration["Jwt:Issuer"] ?? "DomainCopilot";
        _audience = configuration["Jwt:Audience"] ?? "DomainCopilotApp";
        _secretKey = configuration["Jwt:SecretKey"] ?? "ThisIsASecretKeyForDomainCopilotTaskITI2026SecureKey!";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var exp) ? exp : 120;
    }

    public async Task<LoginResponse> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername && u.PasswordHash == request.Password, cancellationToken);

        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid username or password. Please select an existing user from the database or register."
            };
        }

        return GenerateTokenResponse(user.Username, user.FullName, user.Role);
    }

    public async Task<LoginResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var exists = await _dbContext.Users
            .AnyAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (exists)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = $"Username '{request.Username}' is already registered in the database."
            };
        }

        var requestedRole = string.Equals(request.Role, UserRoles.Supervisor, StringComparison.OrdinalIgnoreCase)
            ? UserRoles.Supervisor
            : UserRoles.Technician;

        if (requestedRole == UserRoles.Supervisor)
        {
            var adminExists = await _dbContext.Users.AnyAsync(u => u.Role == UserRoles.Supervisor, cancellationToken);
            if (adminExists)
            {
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "System policy violation: Only one Admin / Supervisor account is permitted. All additional accounts must be Technicians."
                };
            }
        }

        var newUser = new User(
            normalizedUsername,
            request.Password,
            string.IsNullOrWhiteSpace(request.FullName) ? request.Username : request.FullName,
            requestedRole);

        await _dbContext.Users.AddAsync(newUser, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return GenerateTokenResponse(newUser.Username, newUser.FullName, newUser.Role);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .OrderByDescending(u => u.Role) // Supervisors first, then Technicians
            .ThenBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role
            })
            .ToListAsync(cancellationToken);
    }

    private LoginResponse GenerateTokenResponse(string username, string fullName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse
        {
            Success = true,
            Token = tokenString,
            Username = username,
            Role = role,
            ExpiresInSeconds = _expiryMinutes * 60
        };
    }
}
