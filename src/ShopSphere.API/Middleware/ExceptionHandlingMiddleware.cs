using ShopSphere.API.Models;

namespace ShopSphere.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (Exception)
        {
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