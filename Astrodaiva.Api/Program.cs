using Astrodaiva.Api.Data;
using Astrodaiva.Api.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.PostConfigure<AdminAuthOptions>(options =>
{
    options.Password ??= builder.Configuration["ADMIN_PASSWORD"];
    options.PasswordHash ??= builder.Configuration["ADMIN_PASSWORD_HASH"];
    options.TokenSigningKey ??= builder.Configuration["ADMIN_TOKEN_SIGNING_KEY"];
});
builder.Services.AddSingleton<AdminTokenService>();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var configuredOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        var origins = configuredOrigins.Length > 0
            ? configuredOrigins
            : new[]
            {
                "https://krismarcus.github.io",
                "https://astrodaiva-blazor.onrender.com",
                "https://localhost:49283",
                "http://localhost:49284"
            };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();

        // If later you use cookies/auth, you’ll need:
        // .AllowCredentials();
        // and then you cannot use AllowAnyOrigin (but we don't).
    });
});

var conn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(conn))
    throw new InvalidOperationException("ConnectionStrings:Default is missing. Set it in appsettings.json or user-secrets.");

builder.Services.AddDbContext<AstroDbContext>(opt =>
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn)));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ✅ Put CORS before MapControllers (you already do) and before auth if you add it later
app.UseCors("Frontend");

app.Use(async (context, next) =>
{
    if (AllowsAnonymousAccess(context.Request))
    {
        await next();
        return;
    }

    var authHeader = context.Request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Admin authentication is required." });
        return;
    }

    var token = authHeader[bearerPrefix.Length..].Trim();
    var tokenService = context.RequestServices.GetRequiredService<AdminTokenService>();
    if (!tokenService.ValidateToken(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Admin session is invalid or expired." });
        return;
    }

    await next();
});

app.MapControllers();

// Auto-apply migrations on startup (dev-friendly). Remove if you prefer manual.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AstroDbContext>();
    db.Database.Migrate();
}

app.Run();

static bool AllowsAnonymousAccess(HttpRequest request)
{
    if (HttpMethods.IsGet(request.Method) ||
        HttpMethods.IsHead(request.Method) ||
        HttpMethods.IsOptions(request.Method))
    {
        return true;
    }

    return request.Path.Equals("/api/auth/admin/login", StringComparison.OrdinalIgnoreCase);
}
