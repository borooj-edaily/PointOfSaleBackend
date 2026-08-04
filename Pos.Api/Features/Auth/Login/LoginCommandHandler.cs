using Dapper;
using MediatR;
using Pos.Api.Features.Auth.Common;
using Pos.Api.Interfaces;
using Pos.Api.Services;

namespace Pos.Api.Features.Auth.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IPosDatabase _database;
    private readonly JwtService _jwtService;

    public LoginCommandHandler(
        IPosDatabase database,
        JwtService jwtService)
    {
        _database = database;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string sql = """
            SELECT
                Id,
                Username,
                PasswordHash,
                FullName,
                Role,
                IsActive
            FROM Users
            WHERE Username = @Username
            LIMIT 1;
            """;

        var user = await connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { request.Username });

        if (user is null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");

        var validPassword = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash);

        if (!validPassword)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var token = _jwtService.GenerateToken(user);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}