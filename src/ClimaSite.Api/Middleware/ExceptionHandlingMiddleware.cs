using System.Net;
using System.Text.Json;
using ClimaSite.Application.Common.Exceptions;

namespace ClimaSite.Api.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
            ClimaSite.Application.Common.Exceptions.ValidationException validation => (HttpStatusCode.BadRequest, FlattenValidationErrors(validation.Errors)),
            FluentValidation.ValidationException validation => (HttpStatusCode.BadRequest, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            ArgumentException arg => (HttpStatusCode.BadRequest, arg.Message),
            _ => (HttpStatusCode.InternalServerError, "An error occurred processing your request")
        };

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            message = message,
            detail = exception is NotFoundException
                or ClimaSite.Application.Common.Exceptions.ValidationException
                or FluentValidation.ValidationException
                or ArgumentException
                ? exception.Message
                : null
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static string FlattenValidationErrors(IDictionary<string, string[]> errors)
    {
        return string.Join("; ", errors.SelectMany(kvp => kvp.Value));
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
