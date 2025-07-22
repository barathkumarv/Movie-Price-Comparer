using System.Text.Json;
using MoviePriceComparer.Models;
using Prometheus;

namespace MoviePriceComparer.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly Counter ApiErrors = Metrics.CreateCounter(
    "movie_api_errors_total", "Movie API errors",
    ["provider", "error_type"]);

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            ApiErrors.WithLabels("Error occurred", ex.GetType().Name).Inc();
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorMessage = GetErrorMessage(exception)
        };

        var statusCode = GetStatusCode(exception);
        context.Response.StatusCode = statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "External service is currently unavailable",
            TaskCanceledException => "Request timeout - please try again",
            ArgumentException => "Invalid request parameters",
            KeyNotFoundException => "Resource not found",
            UnauthorizedAccessException => "Access denied",
            _ => "An error occurred while processing your request"
        };
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => 502,
            TaskCanceledException => 504,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 401,
            _ => 500
        };
    }
}