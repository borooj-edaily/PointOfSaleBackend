using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Users;

internal static class UserReader
{
    public static async Task<UserDto> ById(
        System.Data.IDbConnection db,
        int id,
        System.Data.IDbTransaction? transaction = null)
    {
        const string userSql = """
            SELECT Id, FullName, Username, Role, IsActive, CreatedAt
            FROM Users
            WHERE Id = @Id;
            """;

        var user = await db.QuerySingleOrDefaultAsync<UserDto>(
            userSql, new { Id = id }, transaction)
            ?? throw new NotFoundException($"User {id} was not found.");

        const string permissionsSql = """
            SELECT p.Name
            FROM Permissions p
            JOIN UserPermissions up ON up.PermissionId = p.Id
            WHERE up.UserId = @Id
            ORDER BY p.Name;
            """;

        user.Permissions.AddRange(
            await db.QueryAsync<string>(permissionsSql, new { Id = id }, transaction));

        return user;
    }
}

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IPosDatabase _database;
    public GetUsersHandler(IPosDatabase database) => _database = database;

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        using var db = _database.Open();
        var ids = await db.QueryAsync<int>("SELECT Id FROM Users ORDER BY FullName;");
        var users = new List<UserDto>();
        foreach (var id in ids)
            users.Add(await UserReader.ById(db, id));
        return users;
    }
}

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IPosDatabase _database;
    public GetUserByIdHandler(IPosDatabase database) => _database = database;

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        using var db = _database.Open();
        return await UserReader.ById(db, request.Id);
    }
}

public sealed class GetPermissionsHandler
    : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
{
    private readonly IPosDatabase _database;
    public GetPermissionsHandler(IPosDatabase database) => _database = database;

    public async Task<List<PermissionDto>> Handle(
        GetPermissionsQuery request, CancellationToken ct)
    {
        using var db = _database.Open();
        return (await db.QueryAsync<PermissionDto>(
            "SELECT Id, Name, Description FROM Permissions ORDER BY Name;"))
            .AsList();
    }
}

