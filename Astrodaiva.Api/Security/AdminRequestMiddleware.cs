namespace Astrodaiva.Api.Security;

public sealed class AdminRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AdminTokenService tokenService)
    {
        if (AllowsAnonymousAccess(context.Request)) { await next(context); return; }
        var header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !tokenService.ValidateToken(header[prefix.Length..].Trim()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Admin login is required or the session has expired." });
            return;
        }
        context.Response.Headers.CacheControl = "no-store";
        await next(context);
    }

    public static bool AllowsAnonymousAccess(HttpRequest request)
    {
        if (HttpMethods.IsOptions(request.Method)) return true;
        if (request.Path.StartsWithSegments("/api/astronomy") || request.Path.StartsWithSegments("/api/import/snapshots")) return false;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) return true;
        return request.Path.Equals("/api/auth/admin/login", StringComparison.OrdinalIgnoreCase);
    }
}
