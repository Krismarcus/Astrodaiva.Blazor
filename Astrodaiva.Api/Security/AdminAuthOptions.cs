namespace Astrodaiva.Api.Security;

public sealed class AdminAuthOptions
{
    public string? Password { get; set; }
    public string? PasswordHash { get; set; }
    public string? TokenSigningKey { get; set; }
    public int TokenLifetimeMinutes { get; set; } = 480;
}
