using System.Text;
using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.AuditLogs;

public sealed class GetAuditLogsHandler
    : IRequestHandler<GetAuditLogsQuery, AuditLogResponse>
{
    private readonly IPosDatabase _database;

    public GetAuditLogsHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<AuditLogResponse> Handle(
        GetAuditLogsQuery request,
        CancellationToken ct)
    {
        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From.Value.Date > request.To.Value.Date)
        {
            throw new Pos.Api.Exceptions.ValidationException(
                "'from' cannot be after 'to'.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var offset = (page - 1) * pageSize;

        var where = new StringBuilder(" WHERE 1 = 1 ");
        var parameters = new DynamicParameters();

        if (request.From.HasValue)
        {
            where.Append(" AND a.CreatedAt >= @From ");
            parameters.Add("From", request.From.Value.Date);
        }

        if (request.To.HasValue)
        {
            where.Append(" AND a.CreatedAt < @ToExclusive ");
            parameters.Add(
                "ToExclusive",
                request.To.Value.Date.AddDays(1));
        }

        if (request.UserId.HasValue)
        {
            where.Append(" AND a.UserId = @UserId ");
            parameters.Add("UserId", request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            where.Append(" AND a.Action = @Action ");
            parameters.Add(
                "Action",
                request.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Entity))
        {
            where.Append(" AND a.Entity = @Entity ");
            parameters.Add(
                "Entity",
                request.Entity.Trim());
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        using var connection = _database.Open();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"""
                SELECT COUNT(*)
                FROM AuditLog a
                {where};
                """,
                parameters,
                cancellationToken: ct));

        var items = await connection.QueryAsync<AuditLogDto>(
            new CommandDefinition(
                $"""
                SELECT
                    a.Id,
                    a.UserId,
                    u.FullName AS UserFullName,
                    a.Action,
                    a.Entity,
                    a.EntityId,
                    CAST(a.Details AS CHAR) AS Details,
                    a.CreatedAt
                FROM AuditLog a
                JOIN Users u ON u.Id = a.UserId
                {where}
                ORDER BY a.CreatedAt DESC, a.Id DESC
                LIMIT @PageSize OFFSET @Offset;
                """,
                parameters,
                cancellationToken: ct));

        return new AuditLogResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.ToList()
        };
    }
}