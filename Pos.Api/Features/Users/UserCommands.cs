using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Users;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IPosDatabase _database;
    public CreateUserHandler(IPosDatabase database) => _database = database;

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var duplicate = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Username = @Username;",
                new { request.Username }, tx);
            if (duplicate != 0)
                throw new DuplicateResourceException("Username already exists.");

            var permissionIds = (request.PermissionIds ?? Array.Empty<int>())
                .Distinct().ToArray();
            await ValidatePermissionIds(db, tx, permissionIds);

            var userId = await db.ExecuteScalarAsync<int>("""
                INSERT INTO Users
                    (FullName, Username, PasswordHash, Role, IsActive, CreatedAt)
                VALUES
                    (@FullName, @Username, @PasswordHash, @Role, TRUE, UTC_TIMESTAMP(6));
                SELECT LAST_INSERT_ID();
                """, new
            {
                request.FullName,
                request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                request.Role
            }, tx);

            foreach (var permissionId in permissionIds)
                await db.ExecuteAsync(
                    "INSERT INTO UserPermissions (UserId, PermissionId) VALUES (@userId, @permissionId);",
                    new { userId, permissionId }, tx);

            var result = await UserReader.ById(db, userId, tx);
            tx.Commit();
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    internal static async Task ValidatePermissionIds(
        System.Data.IDbConnection db,
        System.Data.IDbTransaction tx,
        int[] ids)
    {
        if (ids.Length == 0) return;
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Permissions WHERE Id IN @Ids;",
            new { Ids = ids }, tx);
        if (count != ids.Length)
            throw new Pos.Api.Exceptions.ValidationException(
                "One or more permission IDs are invalid.");
    }
}

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IPosDatabase _database;
    public UpdateUserHandler(IPosDatabase database) => _database = database;

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var current = await db.QuerySingleOrDefaultAsync<UserState>("""
                SELECT Role, IsActive FROM Users WHERE Id = @Id FOR UPDATE;
                """, new { request.Id }, tx)
                ?? throw new NotFoundException($"User {request.Id} was not found.");

            var duplicate = await db.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM Users
                WHERE Username = @Username AND Id <> @Id;
                """, new { request.Username, request.Id }, tx);
            if (duplicate != 0)
                throw new DuplicateResourceException("Username already exists.");

            if (current.Role == "Admin" && request.Role != "Admin" && current.IsActive)
            {
                var activeAdmins = await db.ExecuteScalarAsync<int>("""
                    SELECT COUNT(*) FROM Users
                    WHERE Role = 'Admin' AND IsActive = TRUE;
                    """, transaction: tx);
                if (activeAdmins <= 1)
                    throw new BusinessRuleException(
                        "The last active admin cannot lose the Admin role.");
            }

            await db.ExecuteAsync("""
                UPDATE Users
                SET FullName = @FullName, Username = @Username, Role = @Role
                WHERE Id = @Id;
                """, request, tx);

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                await db.ExecuteAsync("""
                    UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id;
                    """, new
                {
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
                    request.Id
                }, tx);
                await db.ExecuteAsync(
                    "DELETE FROM UserSessions WHERE UserId = @Id;",
                    new { request.Id }, tx);
            }

            var result = await UserReader.ById(db, request.Id, tx);
            tx.Commit();
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private sealed class UserState
    {
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}

public sealed class SetUserPermissionsHandler
    : IRequestHandler<SetUserPermissionsCommand, UserDto>
{
    private readonly IPosDatabase _database;
    public SetUserPermissionsHandler(IPosDatabase database) => _database = database;

    public async Task<UserDto> Handle(
        SetUserPermissionsCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Id = @Id;",
                new { request.Id }, tx);
            if (exists == 0)
                throw new NotFoundException($"User {request.Id} was not found.");

            var permissionIds = request.PermissionIds.Distinct().ToArray();
            await CreateUserHandler.ValidatePermissionIds(db, tx, permissionIds);

            await db.ExecuteAsync(
                "DELETE FROM UserPermissions WHERE UserId = @Id;",
                new { request.Id }, tx);
            foreach (var permissionId in permissionIds)
                await db.ExecuteAsync(
                    "INSERT INTO UserPermissions (UserId, PermissionId) VALUES (@Id, @permissionId);",
                    new { request.Id, permissionId }, tx);

            // Existing token claims may contain old permissions.
            await db.ExecuteAsync(
                "DELETE FROM UserSessions WHERE UserId = @Id;",
                new { request.Id }, tx);

            var result = await UserReader.ById(db, request.Id, tx);
            tx.Commit();
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public sealed class SetUserActiveHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IPosDatabase _database;
    public SetUserActiveHandler(IPosDatabase database) => _database = database;

    public async Task Handle(SetUserActiveCommand request, CancellationToken ct)
    {
        using var db = _database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var user = await db.QuerySingleOrDefaultAsync<UserState>("""
                SELECT Role, IsActive FROM Users WHERE Id = @Id FOR UPDATE;
                """, new { request.Id }, tx)
                ?? throw new NotFoundException($"User {request.Id} was not found.");

            if (!request.IsActive && user.IsActive && user.Role == "Admin")
            {
                var activeAdmins = await db.ExecuteScalarAsync<int>("""
                    SELECT COUNT(*) FROM Users
                    WHERE Role = 'Admin' AND IsActive = TRUE;
                    """, transaction: tx);
                if (activeAdmins <= 1)
                    throw new BusinessRuleException(
                        "The last active admin cannot be deactivated.");
            }

            await db.ExecuteAsync(
                "UPDATE Users SET IsActive = @IsActive WHERE Id = @Id;",
                request, tx);

            if (!request.IsActive)
                await db.ExecuteAsync(
                    "DELETE FROM UserSessions WHERE UserId = @Id;",
                    new { request.Id }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private sealed class UserState
    {
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}

