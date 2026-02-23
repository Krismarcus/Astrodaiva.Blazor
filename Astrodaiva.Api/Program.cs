using Astrodaiva.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "https://krismarcus.github.io",              // ✅ GitHub Pages origin
                "https://astrodaiva-blazor.onrender.com"    // (optional) if you call API from this origin too                
            )
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

app.MapControllers();

// Auto-apply migrations on startup (dev-friendly). Remove if you prefer manual.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AstroDbContext>();
    db.Database.Migrate();
}

app.Run();