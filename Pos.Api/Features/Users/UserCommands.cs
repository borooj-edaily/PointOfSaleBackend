using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;
using Pos.Api.Security;

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

            var permissionIds = request.Role == "Admin"
                ? await GetAllPermissionIds(db, tx)
                : (request.PermissionIds ?? Array.Empty<int>()).Distinct().ToArray();

            if (request.Role != "Admin")
            {
                await ValidatePermissionIds(db, tx, permissionIds);
                await ValidateRolePermissions(db, tx, request.Role, permissionIds);

                // مهما اختار الأدمن، الصلاحيات الإلزامية للدور (مثلاً process_return
                // للكاشير) بتنضاف دايماً — مش رح تضل معلّقة على إن حدا يفتكر يحطّها.
                var mandatoryIds = await GetMandatoryPermissionIds(db, tx, request.Role);
                permissionIds = permissionIds.Union(mandatoryIds).ToArray();
            }

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

    internal static async Task ValidateRolePermissions(
        System.Data.IDbConnection db,
        System.Data.IDbTransaction tx,
        string role,
        int[] permissionIds)
    {
        if (permissionIds.Length == 0) return;
        if (!RolePermissions.AllowedByRole.ContainsKey(role)) return;

        var names = await db.QueryAsync<string>(
            "SELECT Name FROM Permissions WHERE Id IN @Ids;",
            new { Ids = permissionIds }, tx);

        var disallowed = RolePermissions.Disallowed(role, names);
        if (disallowed.Count > 0)
            throw new BusinessRuleException(
                $"Role '{role}' cannot be assigned the following permission(s): " +
                string.Join(", ", disallowed) +
                ". Use the 'Custom' role for non-standard permission combinations.");
    }

    internal static async Task<int[]> GetAllPermissionIds(
        System.Data.IDbConnection db,
        System.Data.IDbTransaction tx)
    {
        var ids = await db.QueryAsync<int>("SELECT Id FROM Permissions;", transaction: tx);
        return ids.ToArray();
    }

    /// <summary>
    /// بيرجع IDs الصلاحيات الإلزامية لدور معيّن (شوف RolePermissions.MandatoryByRole).
    /// بترجع مصفوفة فاضية لو الدور ما إله صلاحيات إلزامية معرّفة.
    /// </summary>
    internal static async Task<int[]> GetMandatoryPermissionIds(
        System.Data.IDbConnection db,
        System.Data.IDbTransaction tx,
        string role)
    {
        if (!RolePermissions.MandatoryByRole.TryGetValue(role, out var names) || names.Length == 0)
            return Array.Empty<int>();

        var ids = await db.QueryAsync<int>(
            "SELECT Id FROM Permissions WHERE Name IN @Names;",
            new { Names = names }, tx);
        return ids.ToArray();
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

            if (request.Role != current.Role &&
                RolePermissions.AllowedByRole.ContainsKey(request.Role))
            {
                var currentPermissionNames = await db.QueryAsync<string>("""
                    SELECT p.Name FROM Permissions p
                    JOIN UserPermissions up ON up.PermissionId = p.Id
                    WHERE up.UserId = @Id;
                    """, new { request.Id }, tx);

                var disallowed = RolePermissions.Disallowed(
                    request.Role, currentPermissionNames);
                if (disallowed.Count > 0)
                    throw new BusinessRuleException(
                        $"Cannot change role to '{request.Role}': the employee " +
                        "currently holds permission(s) not allowed for that role (" +
                        string.Join(", ", disallowed) +
                        "). Update their permissions first.");
            }

            await db.ExecuteAsync("""
                UPDATE Users
                SET FullName = @FullName, Username = @Username, Role = @Role
                WHERE Id = @Id;
                """, request, tx);

            if (request.Role == "Admin")
            {
                var allPermissionIds = await CreateUserHandler.GetAllPermissionIds(db, tx);
                foreach (var permissionId in allPermissionIds)
                    await db.ExecuteAsync("""
                        INSERT IGNORE INTO UserPermissions (UserId, PermissionId)
                        VALUES (@Id, @permissionId);
                        """, new { request.Id, permissionId }, tx);
            }
            else
            {
                // نفس منطق الإلزامية: أي مستخدم (موجود من قبل أو رجع تغيّر دوره)
                // لازم يضل ماسك صلاحياته الإلزامية، حتى لو الشاشة يلي عدّلته ما
                // بعتت permissionIds أصلاً (مثلاً تعديل بيانات بس بدون لمس الصلاحيات).
                var mandatoryIds = await CreateUserHandler.GetMandatoryPermissionIds(db, tx, request.Role);
                foreach (var permissionId in mandatoryIds)
                    await db.ExecuteAsync("""
                        INSERT IGNORE INTO UserPermissions (UserId, PermissionId)
                        VALUES (@Id, @permissionId);
                        """, new { request.Id, permissionId }, tx);
            }

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
            var role = await db.QuerySingleOrDefaultAsync<string>(
                "SELECT Role FROM Users WHERE Id = @Id;",
                new { request.Id }, tx)
                ?? throw new NotFoundException($"User {request.Id} was not found.");

            if (role == "Admin")
                throw new BusinessRuleException(
                    "Admin always has every permission and cannot be restricted. " +
                    "Change the employee's role away from Admin first if you want " +
                    "to limit their access.");

            var permissionIds = request.PermissionIds.Distinct().ToArray();
            await CreateUserHandler.ValidatePermissionIds(db, tx, permissionIds);
            await CreateUserHandler.ValidateRolePermissions(db, tx, role, permissionIds);

            // ما بينقدر حد يشيل صلاحية إلزامية (مثل process_return للكاشير) عن
            // طريق شاشة الصلاحيات — بترضم رجوع تلقائياً حتى لو الأدمن ما اختارها.
            var mandatoryIds = await CreateUserHandler.GetMandatoryPermissionIds(db, tx, role);
            permissionIds = permissionIds.Union(mandatoryIds).ToArray();

            await db.ExecuteAsync(
                "DELETE FROM UserPermissions WHERE UserId = @Id;",
                new { request.Id }, tx);
            foreach (var permissionId in permissionIds)
                await db.ExecuteAsync(
                    "INSERT INTO UserPermissions (UserId, PermissionId) VALUES (@Id, @permissionId);",
                    new { request.Id, permissionId }, tx);

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