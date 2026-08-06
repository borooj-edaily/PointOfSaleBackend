using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Pos.Api.Interfaces;

namespace Pos.Api.Middleware;

public sealed class AuditLogMiddleware
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(
        RequestDelegate next,
        ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IPosDatabase database)
    {
        await _next(context);

        if (!ShouldAudit(context))
            return;

        var userIdValue = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return;

        var segments = context.Request.Path.Value?
            .Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();

        var entity = segments.Length >= 2
            ? segments[1]
            : "unknown";

        var entityId = segments
            .Select(segment =>
                int.TryParse(segment, out var id)
                    ? (int?)id
                    : null)
            .FirstOrDefault(id => id.HasValue);

        var action = ResolveAction(
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty);

        var details = JsonSerializer.Serialize(new
        {
            method = context.Request.Method,
            path = context.Request.Path.Value,
            queryString = context.Request.QueryString.Value,
            statusCode = context.Response.StatusCode,
            ipAddress =
                context.Connection.RemoteIpAddress?.ToString()
        });

        try
        {
            using var connection = database.Open();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO AuditLog
                        (
                            UserId,
                            Action,
                            Entity,
                            EntityId,
                            Details,
                            CreatedAt
                        )
                    VALUES
                        (
                            @UserId,
                            @Action,
                            @Entity,
                            @EntityId,
                            CAST(@Details AS JSON),
                            UTC_TIMESTAMP()
                        );
                    """,
                    new
                    {
                        UserId = userId,
                        Action = action,
                        Entity = entity,
                        EntityId = entityId,
                        Details = details
                    },
                    cancellationToken:
                        context.RequestAborted));
        }
        catch (Exception ex)
        {
            // Audit failure must be visible in logs, but it should not
            // turn an already-successful business request into a 500.
            _logger.LogError(
                ex,
                "Failed to write audit log for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (!MutatingMethods.Contains(context.Request.Method))
            return false;

        if (context.Response.StatusCode >= 400)
            return false;

        // Login is excluded because authentication has not happened yet.
        // Never attempt to record passwords or login request bodies.
        if (context.Request.Path.StartsWithSegments(
                "/api/auth/login"))
        {
            return false;
        }

        return context.User.Identity?.IsAuthenticated == true;
    }

    private static string ResolveAction(
        string method,
        string path)
    {
        if (path.Contains(
                "/shifts/check-in",
                StringComparison.OrdinalIgnoreCase))
        {
            return "SHIFT_CHECK_IN";
        }

        if (path.Contains(
                "/shifts/check-out",
                StringComparison.OrdinalIgnoreCase))
        {
            return "SHIFT_CHECK_OUT";
        }

        if (path.Contains(
                "/auth/logout",
                StringComparison.OrdinalIgnoreCase))
        {
            return "LOGOUT";
        }

        if (path.Contains(
                "/returns",
                StringComparison.OrdinalIgnoreCase))
        {
            return "PROCESS_RETURN";
        }

        if (path.Contains(
                "/exchange",
                StringComparison.OrdinalIgnoreCase))
        {
            return "PROCESS_EXCHANGE";
        }

        if (path.Contains(
                "/finalize",
                StringComparison.OrdinalIgnoreCase))
        {
            return "FINALIZE_INVOICE";
        }

        if (path.Contains(
                "/deactivate",
                StringComparison.OrdinalIgnoreCase))
        {
            return "DEACTIVATE";
        }

        if (path.Contains(
                "/activate",
                StringComparison.OrdinalIgnoreCase))
        {
            return "ACTIVATE";
        }

        return method.ToUpperInvariant() switch
        {
            "POST" => "CREATE",
            "PUT" => "UPDATE",
            "PATCH" => "UPDATE",
            "DELETE" => "DELETE",
            _ => method.ToUpperInvariant()
        };
    }
}