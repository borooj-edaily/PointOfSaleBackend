using MediatR;

namespace Pos.Api.Features.Users;

public sealed class UserDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<string> Permissions { get; init; } = new();
}

public sealed class PermissionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed record GetUsersQuery : IRequest<List<UserDto>>;
public sealed record GetUserByIdQuery(int Id) : IRequest<UserDto>;
public sealed record GetPermissionsQuery : IRequest<List<PermissionDto>>;

public sealed record CreateUserCommand(
    string FullName,
    string Username,
    string Password,
    string Role,
    IReadOnlyCollection<int>? PermissionIds) : IRequest<UserDto>;

public sealed record UpdateUserCommand(
    int Id,
    string FullName,
    string Username,
    string Role,
    string? NewPassword) : IRequest<UserDto>;

public sealed record SetUserPermissionsCommand(
    int Id,
    IReadOnlyCollection<int> PermissionIds) : IRequest<UserDto>;

public sealed record SetUserActiveCommand(int Id, bool IsActive) : IRequest;

