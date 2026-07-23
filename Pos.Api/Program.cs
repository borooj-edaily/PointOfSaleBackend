using System.Reflection;
using DotNetEnv;
using FluentValidation;
using MediatR;
using Pos.Api.Behaviors;
using Pos.Api.Database;
using Pos.Api.Interfaces;
using Pos.Api.Middleware;
using Serilog;

// Load .env before anything else so IConfiguration/Environment can see it.
// .env is git-ignored — never commit real credentials.
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// DotNetEnv loads MYSQL_CONNECTION_STRING as a plain env var, but Dapper/IConfiguration
// looks it up as ConnectionStrings:Default. Bridge the two explicitly here.
var mysqlConnectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(mysqlConnectionString))
{
    builder.Configuration["ConnectionStrings:Default"] = mysqlConnectionString;
}

// ---- CORS (allow the Vite dev server to call this API) ----
const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();