using MediatR;

namespace Pos.Api.Features.Users;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;
public sealed record LogoutCommand(int UserId, string SessionId) : IRequest;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public UserDto User { get; init; } = null!;
}

