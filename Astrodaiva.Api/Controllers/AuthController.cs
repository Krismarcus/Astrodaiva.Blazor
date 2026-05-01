using Astrodaiva.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace Astrodaiva.Api.Controllers;

[ApiController]
[Route("api/auth/admin")]
public sealed class AuthController : ControllerBase
{
    private readonly AdminTokenService _adminTokens;

    public AuthController(AdminTokenService adminTokens)
    {
        _adminTokens = adminTokens;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        if (!_adminTokens.ValidatePassword(request.Password))
            return Unauthorized(new { message = "Invalid admin password." });

        var token = _adminTokens.CreateToken();
        return Ok(new AdminLoginResponse(token.Token, token.ExpiresUtc));
    }

    public sealed record AdminLoginRequest(string? Password);
    public sealed record AdminLoginResponse(string Token, DateTimeOffset ExpiresUtc);
}
