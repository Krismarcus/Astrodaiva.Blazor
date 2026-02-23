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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("https://your-frontend.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

app.UseCors("Frontend");
app.MapControllers();

// Auto-apply migrations on startup (dev-friendly). Remove if you prefer manual.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AstroDbContext>();
    db.Database.Migrate();
}

app.Run();
