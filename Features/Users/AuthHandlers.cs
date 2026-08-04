using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Users;

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IPosDatabase _database;
    private readonly IConfiguration _configuration;

    public LoginHandler(IPosDatabase database, IConfiguration configuration)
        => (_database, _configuration) = (database, configuration);

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var account = await db.QuerySingleOrDefaultAsync<LoginRow>("""
                SELECT Id, Username, PasswordHash, Role, IsActive
                FROM Users
                WHERE Username = @Username
                FOR UPDATE;
                """, new { request.Username }, tx);

            if (account is null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
                throw new Pos.Api.Exceptions.ValidationException(
                    "Invalid username or password.");

            if (!account.IsActive)
                throw new BusinessRuleException("This user account is inactive.");

            var sessionId = Guid.NewGuid().ToString();
            var expiryMinutes = int.TryParse(
                _configuration["Jwt:ExpiryMinutes"], out var configuredMinutes)
                ? configuredMinutes
                : 480;
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Enforces exactly one active session for this account.
            await db.ExecuteAsync(
                "DELETE FROM UserSessions WHERE UserId = @Id;",
                new { account.Id }, tx);
            await db.ExecuteAsync("""
                INSERT INTO UserSessions (Id, UserId, CreatedAt, ExpiresAt)
                VALUES (@sessionId, @Id, UTC_TIMESTAMP(6), @expiresAt);
                """, new { sessionId, account.Id, expiresAt }, tx);

            var permissions = (await db.QueryAsync<string>("""
                SELECT p.Name
                FROM Permissions p
                JOIN UserPermissions up ON up.PermissionId = p.Id
                WHERE up.UserId = @Id
                ORDER BY p.Name;
                """, new { account.Id }, tx)).ToList();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.Username),
                new(ClaimTypes.Role, account.Role),
                new(JwtRegisteredClaimNames.Jti, sessionId)
            };
            claims.AddRange(permissions.Select(
                permission => new Claim("permission", permission)));

            var secret = _configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is missing.");
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    SecurityAlgorithms.HmacSha256));

            var user = await UserReader.ById(db, account.Id, tx);
            tx.Commit();

            return new LoginResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAt,
                User = user
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private sealed class LoginRow
    {
        public int Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IPosDatabase _database;
    public LogoutHandler(IPosDatabase database) => _database = database;

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        await db.ExecuteAsync("""
            DELETE FROM UserSessions
            WHERE Id = @SessionId AND UserId = @UserId;
            """, request);
    }
}

