using System.Reflection;
using DotNetEnv;
using FluentValidation;
using MediatR;
using Pos.Api.Data;
using Pos.Api.Data.Repositories;
using Pos.Api.Middleware;
using Serilog;

// Load .env before anything else so IConfiguration/Environment can see it.
// .env is git-ignored — never commit real credentials.
Env.Load();

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IDbConnectionFactory, MySqlConnectionFactory>();

// TEMPORARY: replace with the real repository in card 7 once Products exists
builder.Services.AddScoped<IProductStockRepository, PlaceholderProductStockRepository>();

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

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();