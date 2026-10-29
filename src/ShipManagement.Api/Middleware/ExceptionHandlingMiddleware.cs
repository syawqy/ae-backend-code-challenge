using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ShipManagement.Api.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Status = (int)statusCode,
            Title = "An unexpected server error occurred."
        };

        switch (exception)
        {
            case KeyNotFoundException knf:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Resource not found.";
                problemDetails.Detail = knf.Message;
                break;

            case ArgumentException ae:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Invalid request argument.";
                problemDetails.Detail = ae.Message;
                break;

            case SqlException sqlEx when sqlEx.Number is 2627 or 2601: // Unique key / PK violation
                statusCode = HttpStatusCode.Conflict;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Duplicate key conflict.";
                problemDetails.Detail = "A record with the specified unique key already exists.";
                break;

            case SqlException sqlEx when sqlEx.Number == 547: // Foreign key / Check constraint
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Integrity constraint violation.";
                problemDetails.Detail = sqlEx.Message;
                break;

            case SqlException sqlEx when sqlEx.Number == 50000: // RAISERROR from stored procedure
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Business rule error.";
                problemDetails.Detail = sqlEx.Message;
                break;

            default:
                problemDetails.Detail = exception.Message;
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
