using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DomainCopilot.Application.Auth.DTOs;
using DomainCopilot.Infrastructure.Auth;
using DomainCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DomainCopilot.UnitTests;

public class JwtAuthTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=domaincopilot;Username=postgres;Password=123456";

    private JwtAuthService CreateAuthService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(options);
        var config = new ConfigurationBuilder().Build();
        return new JwtAuthService(dbContext, config);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsValidToken_ForSeededTechnician()
    {
        var service = CreateAuthService();
        var request = new LoginRequest
        {
            Username = "tech_ahmed",
            Password = "123456"
        };

        var response = await service.AuthenticateAsync(request);

        Assert.True(response.Success);
        Assert.NotNull(response.Token);
        Assert.Equal(UserRoles.Technician, response.Role);

        // Verify claims inside the JWT token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal(UserRoles.Technician, roleClaim.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsValidToken_ForSeededSupervisor()
    {
        var service = CreateAuthService();
        var request = new LoginRequest
        {
            Username = "supervisor",
            Password = "123456"
        };

        var response = await service.AuthenticateAsync(request);

        Assert.True(response.Success);
        Assert.NotNull(response.Token);
        Assert.Equal(UserRoles.Supervisor, response.Role);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal(UserRoles.Supervisor, roleClaim.Value);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsSeededUsers_FromDatabase()
    {
        var service = CreateAuthService();
        var users = await service.GetUsersAsync();

        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Username == "supervisor" && u.Role == UserRoles.Supervisor);
        Assert.Contains(users, u => u.Username == "tech_ahmed" && u.Role == UserRoles.Technician);
    }

    [Fact]
    public async Task RegisterAsync_SavesNewTechnician_ToDatabase()
    {
        var service = CreateAuthService();
        var newUsername = $"tech_auto_{Guid.NewGuid():N}"[..15];
        var request = new RegisterRequest
        {
            Username = newUsername,
            Password = "SecurePassword123!",
            FullName = "Automated Test Technician",
            Role = UserRoles.Technician
        };

        var response = await service.RegisterAsync(request);

        Assert.True(response.Success);
        Assert.Equal(newUsername, response.Username);
        Assert.Equal(UserRoles.Technician, response.Role);
        Assert.NotNull(response.Token);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_OnInvalidPassword()
    {
        var service = CreateAuthService();
        var request = new LoginRequest
        {
            Username = "supervisor",
            Password = "WrongPassword999!"
        };

        var response = await service.AuthenticateAsync(request);

        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.Contains("Invalid", response.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_RejectsSecondAdmin_WhenAdminAlreadyExists()
    {
        var service = CreateAuthService();
        var request = new RegisterRequest
        {
            Username = "second_supervisor",
            Password = "Password123!",
            FullName = "Second Admin Attempt",
            Role = UserRoles.Supervisor
        };

        var response = await service.RegisterAsync(request);

        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.Contains("Only one Admin / Supervisor account is permitted", response.ErrorMessage);
    }
}
