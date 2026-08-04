using System.Reflection;
using System.Security.Claims;
using System.Text;
using Dapper;
using DotNetEnv;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Pos.Api.Behaviors;
using Pos.Api.Database;
using Pos.Api.Interfaces;
using Pos.Api.Middleware;
using Pos.Api.Security;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["ConnectionStrings:Default"] =
    Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
builder.Configuration["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET");
builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
builder.Configuration["Jwt:ExpiryMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES");

// ---- Serilog ----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ---- Controllers + Swagger ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- Dapper / MySQL (NOT EF Core) ----
builder.Services.AddSingleton<IPosDatabase, PosDatabase>();

// ---- MediatR ----
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ---- FluentValidation ----
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ---- Users authentication and operation-level authorization ----
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT_SECRET is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionId = context.Principal?.FindFirstValue("jti");
                if (!int.TryParse(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    context.Fail("Invalid session claims.");
                    return;
                }

                var database = context.HttpContext.RequestServices
                    .GetRequiredService<IPosDatabase>();
                using var connection = database.Open();
                var valid = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM UserSessions s
                    JOIN Users u ON u.Id = s.UserId
                    WHERE s.Id = @SessionId
                      AND s.UserId = @UserId
                      AND s.ExpiresAt > UTC_TIMESTAMP(6)
                      AND u.IsActive = TRUE;",
                    new { SessionId = sessionId, UserId = parsedUserId });

                if (valid == 0)
                    context.Fail("Session is no longer active.");
            }
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissions.ManageUsers, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(Permissions.ManageUsers)));
});

// ---- CORS ----
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
