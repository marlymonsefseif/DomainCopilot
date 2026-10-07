namespace DomainCopilot.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public string Role { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
        Role = string.Empty;
    }

    public User(
        string username,
        string passwordHash,
        string fullName,
        string role)
    {
        Id = Guid.NewGuid();
        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}
