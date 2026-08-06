using MediatR;

namespace Pos.Api.Features.AuditLogs;

public sealed class AuditLogDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public int? EntityId { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class AuditLogResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public List<AuditLogDto> Items { get; init; } = new();
}

public sealed record GetAuditLogsQuery(
    DateTime? From,
    DateTime? To,
    int? UserId,
    string? Action,
    string? Entity,
    int Page,
    int PageSize) : IRequest<AuditLogResponse>;