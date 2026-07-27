using System.Net;
using System.Text.Json;
using Pos.Api.Common;
using Pos.Api.Exceptions;

namespace Pos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for {Path}", context.Request.Path);
            await WriteResponseAsync(context, (int)HttpStatusCode.BadRequest, "Validation failed.", ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found for {Path}", context.Request.Path);
            await WriteResponseAsync(context, (int)HttpStatusCode.NotFound, ex.Message, new List<string> { ex.Message });
        }
        catch (DuplicateResourceException ex)
        {
            _logger.LogWarning(ex, "Duplicate resource for {Path}", context.Request.Path);
            await WriteResponseAsync(context, (int)HttpStatusCode.Conflict, ex.Message, new List<string> { ex.Message });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business rule violation for {Path}", context.Request.Path);
            await WriteResponseAsync(context, (int)HttpStatusCode.Conflict, ex.Message, new List<string> { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteResponseAsync(context, (int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", new List<string>());
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, List<string> errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiErrorResponse
        {
            Success = false,
            Timestamp = DateTime.UtcNow,
            Message = message,
            Errors = errors
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
