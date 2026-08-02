using MediatR;

namespace Pos.Api.Features.Auth.Login;

public sealed record LoginCommand(
    string Username,
    string Password
) : IRequest<LoginResponse>;