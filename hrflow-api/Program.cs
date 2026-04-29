using System.Text;
using HrFlow.Api.Data;
using HrFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var dbProvider = (Environment.GetEnvironmentVariable("DB_PROVIDER") ?? builder.Configuration["Db:Provider"] ?? "sqlite").Trim().ToLowerInvariant();
var connectionString = dbProvider switch
{
    "sqlserver" => builder.Configuration.GetConnectionString("SqlServer"),
    "mysql" => builder.Configuration.GetConnectionString("MySql"),
    _ => builder.Configuration.GetConnectionString("Sqlite")
};

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (dbProvider)
    {
        case "sqlserver":
            options.UseSqlServer(connectionString);
            break;
        case "mysql":
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;
        default:
            options.UseSqlite(connectionString);
            break;
    }
});

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var includeErrorDetails = builder.Configuration.GetValue<bool>("Diagnostics:IncludeErrorDetails");
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = includeErrorDetails ? ex.ToString() : "Unexpected server error."
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (string.Equals(dbProvider, "sqlite", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Environment.GetEnvironmentVariable("RESET_DB"), "1", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.EnsureDeletedAsync();
    }
    await db.Database.EnsureCreatedAsync();
    await SeedData.EnsureSeededAsync(db, scope.ServiceProvider.GetRequiredService<PasswordHasher>());
}

app.Run();

namespace HrFlow.Api
{
    public partial class Program { }
}
