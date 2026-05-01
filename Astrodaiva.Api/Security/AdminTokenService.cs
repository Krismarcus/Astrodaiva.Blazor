using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Astrodaiva.Api.Security;

public sealed class AdminTokenService
{
    private const string PasswordHashPrefix = "pbkdf2-sha256";
    private readonly AdminAuthOptions _options;

    public AdminTokenService(IOptions<AdminAuthOptions> options)
    {
        _options = options.Value;
    }

    public bool ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        if (!string.IsNullOrWhiteSpace(_options.PasswordHash))
            return VerifyPasswordHash(password, _options.PasswordHash);

        if (string.IsNullOrWhiteSpace(_options.Password))
            return false;

        return FixedTimeEquals(password, _options.Password);
    }

    public AdminTokenResult CreateToken()
    {
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(GetTokenLifetimeMinutes());
        var payload = new AdminTokenPayload(
            Subject: "admin",
            ExpiresUnixTimeSeconds: expiresUtc.ToUnixTimeSeconds(),
            Nonce: Base64UrlEncode(RandomNumberGenerator.GetBytes(16)));

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signaturePart = Sign(payloadPart);

        return new AdminTokenResult($"{payloadPart}.{signaturePart}", expiresUtc);
    }

    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
            return false;

        string expectedSignature;
        try
        {
            expectedSignature = Sign(parts[0]);
        }
        catch
        {
            return false;
        }

        if (!FixedTimeEquals(parts[1], expectedSignature))
            return false;

        AdminTokenPayload? payload;
        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            payload = JsonSerializer.Deserialize<AdminTokenPayload>(payloadJson);
        }
        catch
        {
            return false;
        }

        if (payload is null || payload.Subject != "admin")
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return payload.ExpiresUnixTimeSeconds > now;
    }

    private int GetTokenLifetimeMinutes()
        => Math.Clamp(_options.TokenLifetimeMinutes <= 0 ? 480 : _options.TokenLifetimeMinutes, 15, 1440);

    private string Sign(string payloadPart)
    {
        var key = GetSigningKey();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart)));
    }

    private string GetSigningKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.TokenSigningKey))
            return _options.TokenSigningKey;

        if (!string.IsNullOrWhiteSpace(_options.PasswordHash))
            return _options.PasswordHash;

        if (!string.IsNullOrWhiteSpace(_options.Password))
            return _options.Password;

        throw new InvalidOperationException("Admin token signing key is not configured.");
    }

    private static bool VerifyPasswordHash(string password, string passwordHash)
    {
        var parts = passwordHash.Split(':', 4);
        if (parts.Length != 4 || !parts[0].Equals(PasswordHashPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(parts[1], out var iterations) || iterations < 10_000)
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;
        if (padding > 0)
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');

        return Convert.FromBase64String(base64);
    }

    private sealed record AdminTokenPayload(string Subject, long ExpiresUnixTimeSeconds, string Nonce);
}

public sealed record AdminTokenResult(string Token, DateTimeOffset ExpiresUtc);
