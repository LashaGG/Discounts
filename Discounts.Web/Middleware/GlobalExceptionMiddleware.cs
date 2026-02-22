using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Discounts.Web.Middleware;

/// <summary>
/// Global exception handler that returns standard ProblemDetails responses.
/// Controllers should NOT use try/catch — exceptions bubble up here.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Validation Error"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access Denied"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid Operation"),
            ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid Argument"),
            _ => (HttpStatusCode.InternalServerError, "Server Error")
        };

        // For MVC/Razor views — redirect to error page for non-API requests
        if (!IsApiRequest(context))
        {
            if (exception is ValidationException validationException)
            {
                // For validation errors in MVC, store in TempData and redirect back
                context.Response.StatusCode = (int)statusCode;
                var errors = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage));

                if (context.Request.HasFormContentType)
                {
                    // Store errors and redirect back
                    context.Response.Redirect(context.Request.Headers.Referer.FirstOrDefault() ?? "/");
                    return;
                }
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.Redirect($"/Home/Error?statusCode={(int)statusCode}");
            return;
        }

        // For API requests — return ProblemDetails JSON
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails).ConfigureAwait(false);
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api") ||
               context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true);
    }
}
