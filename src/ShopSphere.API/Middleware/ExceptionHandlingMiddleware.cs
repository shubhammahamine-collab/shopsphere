using Microsoft.EntityFrameworkCore;
using ShopSphere.API.Models;

namespace ShopSphere.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Validation error while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid business operation while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status404NotFound,
                ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Database concurrency conflict while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                "The product stock was updated by another request. Please retry the order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = message
            });
    }
}